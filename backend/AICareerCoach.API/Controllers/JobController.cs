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

        public JobController(IJobService jobService, IJobRecommendationService jobRecommendationService)
        {
            _jobService = jobService;
            _jobRecommendationService = jobRecommendationService;
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
        public async Task<ActionResult<JobDto>> Create(CreateJobDto dto)
        {
            var job = await _jobService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
        }

        #region Index Jobs Embeddings (Admin Only)
        [HttpPost("index-embeddings")]
        // [Authorize(Roles = "Admin")] 
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
        //[Authorize] 
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
