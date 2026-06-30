using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.BLL.Interfaces.External;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services.AI
{
    public class JobSyncService : IJobSyncService
    {
        private readonly IJobProvider _jobProvider;
        private readonly ISkillExtractionService _skillExtractor;
        private readonly IEmbeddingService _embeddingService;
        private readonly AICareerCoachDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<JobSyncService> _logger;

        public JobSyncService(
            IJobProvider jobProvider,
            ISkillExtractionService skillExtractor,
            IEmbeddingService embeddingService,
            AICareerCoachDbContext context,
            IConfiguration config,
            ILogger<JobSyncService> logger)
        {
            _jobProvider = jobProvider;
            _skillExtractor = skillExtractor;
            _embeddingService = embeddingService;
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<SyncResultDto> SyncAsync(CancellationToken ct)
        {
            var result = new SyncResultDto
            {
                SyncedAt = DateTime.UtcNow
            };

            var countries = _config.GetSection("Jooble:Countries").Get<string[]>() ?? new[] { "Egypt", "UAE" };
            var maxPages = int.TryParse(_config["Jooble:MaxPagesPerCountry"], out var mp) ? mp : 3;
            var providerName = _jobProvider.GetType().Name.Replace("JobProvider", "");

            var allFetched = new List<JobFetchResultDto>();

            foreach (var country in countries)
            {
                try
                {
                    var fetched = await _jobProvider.FetchJobsAsync(country, maxPages, ct);
                    allFetched.AddRange(fetched);
                    result.Fetched += fetched.Count;
                    _logger.LogInformation("Fetched {Count} jobs from {Provider} {Country}.", fetched.Count, providerName, country);
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    var msg = $"Country {country}: {ex.Message}";
                    result.ErrorMessages.Add(msg);
                    _logger.LogError(ex, "{Provider} fetch failed for {Country}.", providerName, country);
                }
            }

            if (allFetched.Count == 0)
            {
                _logger.LogInformation("{Provider} returned 0 jobs overall.", providerName);
                return result;
            }

            var externalIds = allFetched.Select(j => j.ExternalId).Where(id => !string.IsNullOrEmpty(id)).ToList();

            var existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (externalIds.Count > 0)
            {
                existingIds = (await _context.Jobs
                    .Where(j => j.Source == providerName && j.ExternalId != null && externalIds.Contains(j.ExternalId))
                    .Select(j => j.ExternalId!)
                    .ToListAsync(ct))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            var newJobs = allFetched.Where(j => !existingIds.Contains(j.ExternalId)).ToList();
            result.Skipped = allFetched.Count - newJobs.Count;

            if (newJobs.Count == 0)
            {
                _logger.LogInformation("All {Fetched} fetched jobs already exist in DB.", result.Fetched);
                return result;
            }

            try
            {
                var skillsMap = await _skillExtractor.ExtractSkillsBatchAsync(
                    newJobs.Select(j => (j.ExternalId, j.Title, j.Description)).ToList(),
                    ct);

                foreach (var fetched in newJobs)
                {
                    var skills = skillsMap.TryGetValue(fetched.ExternalId, out var s) && s.Count > 0
                        ? s
                        : new List<string>();

                    var combinedText = $"Title: {fetched.Title}\nCompany: {fetched.Company}\nDescription: {fetched.Description}\nSkills: {string.Join(", ", skills)}";

                    var embedding = await _embeddingService.GenerateEmbeddingAsync(combinedText);

                    var salary = fetched.SalaryMax > 0 ? fetched.SalaryMax : fetched.SalaryMin;

                    var isRemote = (fetched.Title ?? string.Empty).Contains("remote", StringComparison.OrdinalIgnoreCase)
                        || (fetched.Location ?? string.Empty).Contains("remote", StringComparison.OrdinalIgnoreCase);

                    var job = new Job
                    {
                        Title = fetched.Title,
                        Company = fetched.Company,
                        Description = fetched.Description,
                        RequiredSkills = JsonSerializer.Serialize(skills),
                        Location = fetched.Location,
                        Salary = salary,
                        PostedAt = fetched.Created == default ? DateTime.UtcNow : fetched.Created,
                        ExternalId = fetched.ExternalId,
                        Source = providerName,
                        ExternalUrl = fetched.RedirectUrl,
                        ContractType = fetched.ContractType,
                        Category = fetched.Category,
                        IsRemote = isRemote,
                        CompanyLogoUrl = fetched.CompanyLogoUrl
                    };

                    _context.Jobs.Add(job);
                    await _context.SaveChangesAsync(ct);

                    var jobEmbedding = new JobEmbedding
                    {
                        JobId = job.Id,
                        Embedding = embedding,
                        ComputedAt = DateTime.UtcNow
                    };
                    _context.JobEmbeddings.Add(jobEmbedding);
                    await _context.SaveChangesAsync(ct);

                    // Rate-limit mitigation: GitHub Models free tier is 24 req/min/user.
                    // 3s delay between embedding calls → 60 jobs in 180s = 3 min
                    // (well under the 24/60s sliding limit).
                    await Task.Delay(3000, ct);

                    result.New++;
                    result.Embedded++;
                }
            }
            catch (Exception ex)
            {
                result.Errors++;
                var msg = $"Insert phase: {ex.Message}";
                result.ErrorMessages.Add(msg);
                _logger.LogError(ex, "JobSyncService insert phase failed.");
                throw;
            }

            _logger.LogInformation(
                "Sync complete: fetched={Fetched} new={New} skipped={Skipped} embedded={Embedded} errors={Errors}",
                result.Fetched, result.New, result.Skipped, result.Embedded, result.Errors);

            return result;
        }

        public async Task<SyncStatusDto> GetStatusAsync()
        {
            var lastLog = await _context.JobSyncLogs
                .OrderByDescending(l => l.SyncedAt)
                .FirstOrDefaultAsync();

            var enabled = bool.TryParse(_config["JobsSync:Enabled"], out var e) && e;
            var intervalHours = double.TryParse(_config["JobsSync:IntervalHours"], out var h) ? h : 24.0;

            return new SyncStatusDto
            {
                LastSyncAt = lastLog?.SyncedAt,
                LastSyncNew = lastLog?.New,
                LastSyncErrors = lastLog?.Errors,
                Enabled = enabled,
                IntervalHours = intervalHours
            };
        }
    }
}
