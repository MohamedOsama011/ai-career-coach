using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Helpers;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services.AI
{
    public class JobRecommendationService : IJobRecommendationService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILlmExplanationService _llmExplanationService;
        private readonly ISubscriptionGateService _gateService;
        private readonly ILogger<JobRecommendationService> _logger;

        public JobRecommendationService(
            AICareerCoachDbContext context,
            IEmbeddingService embeddingService,
            ILlmExplanationService llmExplanationService,
            ISubscriptionGateService gateService,
            ILogger<JobRecommendationService> logger)
        {
            _context = context;
            _embeddingService = embeddingService;
            _llmExplanationService = llmExplanationService;
            _gateService = gateService;
            _logger = logger;
        }

        #region Index Jobs Embeddings (Admin Only)
        public async Task IndexJobsAsync()
        {
            _logger.LogInformation("Starting jobs indexing and embedding generation...");

            var jobs = await _context.Jobs.ToListAsync();

            foreach (var job in jobs)
            {
                var existingEmbedding = await _context.JobEmbeddings.FirstOrDefaultAsync(je => je.JobId == job.Id);

                if (existingEmbedding != null)
                {
                    _logger.LogInformation($"Job {job.Id} already has an embedding. Skipping...");
                    continue;
                }
                _logger.LogInformation($"Generating fresh embedding for Job {job.Id}...");

                string cleanedSkills = CleanSkillsJson(job.RequiredSkills);
                string combinedText = $"Title: {job.Title}\nCompany: {job.Company}\nDescription: {job.Description}\nSkills: {cleanedSkills}";

                var embeddingVector = await _embeddingService.GenerateEmbeddingAsync(combinedText);

                    var newEmbedding = new JobEmbedding
                    {
                        JobId = job.Id,
                        Embedding = embeddingVector, 
                        ComputedAt = DateTime.UtcNow
                    };
                    await _context.JobEmbeddings.AddAsync(newEmbedding);
                

                await Task.Delay(4500);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("All jobs indexed successfully!");
        }
        #endregion

        #region Get Recommendations (User Flow)
        public async Task<JobRecommendationResultDto> GetRecommendationsAsync(string userId)
        {
            var cv = await _context.CVs
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.UploadedAt)
                .FirstOrDefaultAsync()
                ?? throw new KeyNotFoundException("Please upload your CV first to get personalized job recommendations.");

            string cvText = cv.ExtractedData;

            if (string.IsNullOrEmpty(cvText))
                throw new InvalidOperationException("CV text not extracted yet. Please request a CV feedback first.");

            string cvHash = ComputeMd5Hash(cvText);

            var cachedResult = await _context.JobRecommendationCaches
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CvHash == cvHash);

            JobRecommendationResultDto fullResult;
            if (cachedResult != null)
            {
                _logger.LogInformation("Cache hit! Returning job recommendations from cache.");
                fullResult = JsonSerializer.Deserialize<JobRecommendationResultDto>(cachedResult.RecommendationsJson)!;
            }
            else
            {
                _logger.LogInformation("Cache miss! Computing fresh recommendations via RAG pipeline...");
                fullResult = await ComputeFreshRecommendationsAsync(userId, cvText);

                var newCache = new JobRecommendationCache
                {
                    UserId = userId,
                    CvHash = cvHash,
                    RecommendationsJson = JsonSerializer.Serialize(fullResult),
                    CreatedAt = DateTime.UtcNow
                };

                await _context.JobRecommendationCaches.AddAsync(newCache);
                await _context.SaveChangesAsync();
            }

            return await ApplyFreeTierGateAsync(userId, fullResult);
        }

        private async Task<JobRecommendationResultDto> ComputeFreshRecommendationsAsync(string userId, string cvText)
        {
            var cvEmbedding = await _embeddingService.GenerateEmbeddingAsync(cvText);

            var allJobEmbeddings = await _context.JobEmbeddings.Include(je => je.Job).ToListAsync();

            if (!allJobEmbeddings.Any())
                throw new InvalidOperationException("No job models found. Please ask administration to sync the vector store.");

            var matchedJobsList = allJobEmbeddings.Select(je =>
            {
                double similarityScore = ComputeCosineSimilarity(cvEmbedding, je.Embedding);

                int matchPercentage = (int)Math.Clamp(Math.Round(similarityScore * 100), 0, 100);

                return new
                {
                    JobEntity = je.Job,
                    Percentage = matchPercentage
                };
            })
            .OrderByDescending(mj => mj.Percentage)
            .Take(5)
            .ToList();

            var topJobEntities = matchedJobsList.Select(mj => mj.JobEntity).ToList();
            var aiExplanations = await _llmExplanationService.GenerateExplanationsAsync(cvText, topJobEntities);

            var recommendationsList = matchedJobsList.Select(mj => new JobRecommendationDto
            {
                JobId = mj.JobEntity.Id,
                Title = mj.JobEntity.Title,
                Company = mj.JobEntity.Company,
                Description = HtmlHelper.StripHtml(mj.JobEntity.Description),
                CompanyLogoUrl = mj.JobEntity.CompanyLogoUrl,
                Salary = mj.JobEntity.Salary,
                Location = mj.JobEntity.Location,
                ExternalUrl = mj.JobEntity.ExternalUrl,
                MatchScore = mj.Percentage,
                MatchExplanation = aiExplanations.TryGetValue(mj.JobEntity.Id, out var explanation)
                    ? explanation.Explanation
                    : "Your background matches the essential criteria for this role.",
                MissingSkills = aiExplanations.TryGetValue(mj.JobEntity.Id, out var missing)
                    ? missing.MissingSkills
                    : new List<string>()
            }).ToList();

            return new JobRecommendationResultDto
            {
                UserId = userId,
                Recommendations = recommendationsList,
                GeneratedAt = DateTime.UtcNow,
                TotalCount = recommendationsList.Count,
                ReturnedCount = recommendationsList.Count,
                IsLimited = false,
            };
        }

        private async Task<JobRecommendationResultDto> ApplyFreeTierGateAsync(string userId, JobRecommendationResultDto full)
        {
            var hasActive = await _gateService.HasActiveSubscriptionAsync(userId);
            var total = full.Recommendations.Count;

            if (hasActive || total == 0)
            {
                full.IsLimited = false;
                full.TotalCount = total;
                full.ReturnedCount = total;
                return full;
            }

            var allowed = Math.Min(total, FreeLimits.JobRecommendations);
            full.Recommendations = full.Recommendations.Take(allowed).ToList();
            full.IsLimited = true;
            full.TotalCount = total;
            full.ReturnedCount = allowed;
            _logger.LogInformation("Gate: User {UserId} free tier — returning {Allowed}/{Total} recommendations", userId, allowed, total);
            return full;
        }
        #endregion

        #region Helpers

        private static double ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
        {
            if (vectorA.Length != vectorB.Length) return 0;

            double dotProduct = 0, magnitudeA = 0, magnitudeB = 0;

            for (int i = 0; i < vectorA.Length; i++)
            {
                dotProduct += vectorA[i] * vectorB[i];
                magnitudeA += vectorA[i] * vectorA[i];
                magnitudeB += vectorB[i] * vectorB[i];
            }

            if (magnitudeA == 0 || magnitudeB == 0) return 0;

            return dotProduct / (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
        }
        private static string ComputeMd5Hash(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = MD5.HashData(inputBytes);
            return Convert.ToHexString(hashBytes);
        }

        private static string CleanSkillsJson(string skillsJson)
        {
            if (string.IsNullOrWhiteSpace(skillsJson)) return string.Empty;
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(skillsJson);
                return list != null ? string.Join(", ", list) : skillsJson;
            }
            catch
            {
                return skillsJson; 
            }
        }
        #endregion
    }
}
