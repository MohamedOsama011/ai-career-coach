using AICareerCoach.BLL.DTOs.Common;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IJobRecommendationService _jobRecommendationService;
        private readonly IJobSyncService _jobSyncService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<JobController> _logger;

        public JobController(
            IJobService jobService,
            IJobRecommendationService jobRecommendationService,
            IJobSyncService jobSyncService,
            IServiceScopeFactory scopeFactory,
            ILogger<JobController> logger)
        {
            _jobService = jobService;
            _jobRecommendationService = jobRecommendationService;
            _jobSyncService = jobSyncService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<JobDto>>> GetAll([FromQuery] JobFilterDto filter)
        {
            var result = await _jobService.GetJobsAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<JobDto>> GetById(int id)
        {
            try
            {
                var job = await _jobService.GetByIdAsync(id);
                return Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<JobDto>> Create(CreateJobDto dto)
        {
            var job = await _jobService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<JobDto>> Update(int id, [FromBody] UpdateJobDto dto)
        {
            try
            {
                var job = await _jobService.UpdateAsync(id, dto);
                return Ok(job);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _jobService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("sync")]
        [Authorize(Roles = "Admin")]
        public ActionResult<object> SyncJobs()
        {
            var startedAt = DateTime.UtcNow;
            var scopeFactory = _scopeFactory;

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var syncService = scope.ServiceProvider.GetRequiredService<IJobSyncService>();
                    var context = scope.ServiceProvider.GetRequiredService<AICareerCoachDbContext>();

                    var result = await syncService.SyncAsync(CancellationToken.None);
                    var duration = DateTime.UtcNow - startedAt;

                    var log = new JobSyncLog
                    {
                        SyncedAt = result.SyncedAt,
                        Fetched = result.Fetched,
                        New = result.New,
                        Skipped = result.Skipped,
                        Embedded = result.Embedded,
                        Errors = result.Errors,
                        ErrorMessages = result.ErrorMessages.Count > 0
                            ? JsonSerializer.Serialize(result.ErrorMessages)
                            : null,
                        Duration = duration
                    };
                    context.JobSyncLogs.Add(log);
                    await context.SaveChangesAsync(CancellationToken.None);

                    _logger.LogInformation(
                        "Manual sync complete: fetched={Fetched} new={New} skipped={Skipped} embedded={Embedded} errors={Errors} duration={Duration}ms",
                        result.Fetched, result.New, result.Skipped, result.Embedded, result.Errors, duration.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background sync failed.");
                }
            });

            return Ok(new { status = "started", message = "Job sync started. Results will appear in sync history once complete." });
        }

        [HttpGet("sync-status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SyncStatusDto>> SyncStatus()
        {
            return Ok(await _jobSyncService.GetStatusAsync());
        }

        #region Index Jobs Embeddings (Admin Only)
        [HttpPost("index-embeddings")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> IndexJobsEmbeddings()
        {
            try
            {
                await _jobRecommendationService.IndexJobsAsync();

                return Ok(new { message = "All jobs have been embedded and synced into SQL Server successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while indexing jobs.", error = ex.Message });
            }
        }
        #endregion

        #region Get Recommendations (User Flow)
        [HttpGet("recommendations")]
        [Authorize]
        public async Task<ActionResult<JobRecommendationResultDto>> GetJobRecommendations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User identity could not be verified from the token." });
            }

            try
            {
                var result = await _jobRecommendationService.GetRecommendationsAsync(userId);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while processing recommendations.", error = ex.Message });
            }
        }
        #endregion
    }
}
