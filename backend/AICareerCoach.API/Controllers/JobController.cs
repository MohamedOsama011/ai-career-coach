using AICareerCoach.BLL.DTOs.Common;
using AICareerCoach.BLL.DTOs.Job;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly IJobService _jobService;
        private readonly IJobRecommendationService _jobRecommendationService;
        private readonly IJobSyncService _jobSyncService;

        public JobController(
            IJobService jobService,
            IJobRecommendationService jobRecommendationService,
            IJobSyncService jobSyncService)
        {
            _jobService = jobService;
            _jobRecommendationService = jobRecommendationService;
            _jobSyncService = jobSyncService;
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
        public async Task<ActionResult<SyncResultDto>> SyncJobs()
        {
            try
            {
                var result = await _jobSyncService.SyncAsync(HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while syncing jobs.", error = ex.Message });
            }
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
