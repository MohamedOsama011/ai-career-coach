using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services
{
    public class JobSyncHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<JobSyncHostedService> _logger;

        public JobSyncHostedService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<JobSyncHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = bool.TryParse(_config["JobsSync:Enabled"], out var e) && e;
            if (!enabled)
            {
                _logger.LogInformation("JobSyncHostedService disabled (JobsSync:Enabled=false).");
                return;
            }

            var initialDelaySeconds = int.TryParse(_config["JobsSync:InitialDelaySeconds"], out var d) ? d : 30;
            var intervalHours = double.TryParse(_config["JobsSync:IntervalHours"], out var h) ? h : 24.0;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(initialDelaySeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await TryFirstRunSeedAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await RunSyncAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("JobSyncHostedService stopping.");
            }
        }

        private async Task TryFirstRunSeedAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AICareerCoachDbContext>();

            bool isEmpty;
            try
            {
                isEmpty = !await context.Jobs.AnyAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check Jobs table state on startup.");
                return;
            }

            if (!isEmpty)
            {
                _logger.LogInformation("Jobs table already populated; skipping first-run seed.");
                return;
            }

            _logger.LogInformation("Jobs table empty. Attempting first-run Adzuna sync...");

            using var syncScope = _scopeFactory.CreateScope();
            var syncService = syncScope.ServiceProvider.GetRequiredService<IJobSyncService>();

            SyncOutcome outcome;
            try
            {
                var result = await syncService.SyncAsync(ct);
                outcome = new SyncOutcome
                {
                    Success = result.New > 0,
                    Fetched = result.Fetched,
                    New = result.New,
                    Skipped = result.Skipped,
                    Embedded = result.Embedded,
                    Errors = result.Errors,
                    ErrorMessages = string.Join("\n", result.ErrorMessages)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "External job sync failed on first run.");
                outcome = new SyncOutcome { Success = false, ErrorMessages = ex.Message };
            }

            if (!outcome.Success)
            {
                _logger.LogWarning("External job sync returned 0 jobs (or failed). Falling back to JobSeeder.");
                try
                {
                    await JobSeeder.SeedAsync(context);
                    _logger.LogInformation("JobSeeder fallback completed.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JobSeeder fallback also failed. Jobs table is empty.");
                }
            }
            else
            {
                _logger.LogInformation("First sync success: seeded {N} real jobs from external provider.", outcome.New);
            }
        }

        private async Task RunSyncAsync(CancellationToken ct)
        {
            var startedAt = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IJobSyncService>();
            var context = scope.ServiceProvider.GetRequiredService<AICareerCoachDbContext>();

            try
            {
                var result = await syncService.SyncAsync(ct);
                var duration = DateTime.UtcNow - startedAt;

                var log = new JobSyncLog
                {
                    SyncedAt = result.SyncedAt,
                    Fetched = result.Fetched,
                    New = result.New,
                    Skipped = result.Skipped,
                    Embedded = result.Embedded,
                    Errors = result.Errors,
                    ErrorMessages = result.ErrorMessages.Count > 0 ? JsonSerializer.Serialize(result.ErrorMessages) : null,
                    Duration = duration
                };
                context.JobSyncLogs.Add(log);
                await context.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Periodic sync complete: fetched={Fetched} new={New} skipped={Skipped} embedded={Embedded} errors={Errors} duration={Duration}ms",
                    result.Fetched, result.New, result.Skipped, result.Embedded, result.Errors, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Periodic sync failed.");
            }
        }

        private class SyncOutcome
        {
            public bool Success { get; set; }
            public int Fetched { get; set; }
            public int New { get; set; }
            public int Skipped { get; set; }
            public int Embedded { get; set; }
            public int Errors { get; set; }
            public string? ErrorMessages { get; set; }
        }
    }
}
