using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AICareerCoach.API.Controllers
{
    // [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class RoadmapController : ControllerBase
    {
        private readonly IRoadmapService _roadmapService;
        private readonly IUserRoadmapService _userRoadmapService;
        private readonly ISubscriptionGateService _gateService;

        public RoadmapController(
            IRoadmapService roadmapService,
            IUserRoadmapService userRoadmapService,
            ISubscriptionGateService gateService)
        {
            _roadmapService = roadmapService;
            _userRoadmapService = userRoadmapService;
            _gateService = gateService;
        }

        [HttpGet]
        public async Task<ActionResult<List<RoadmapDto>>> GetAll([FromQuery] string? track)
        {
            var result = await _roadmapService.GetAllAsync(track);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoadmapDto>> GetById(int id)
        {
            try
            {
                var result = await _roadmapService.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<RoadmapDto>> Create(CreateRoadmapDto dto)
        {
            var result = await _roadmapService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPost("index-template-embeddings")]
        public async Task<IActionResult> IndexTemplateEmbeddings()
        {
            try
            {
                await _roadmapService.IndexTemplateEmbeddingsAsync();
                return Ok(new { message = "All roadmap templates have been embedded successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while indexing template embeddings.", error = ex.Message });
            }
        }

        [HttpPost("generate")]
        //[Authorize]
        public async Task<ActionResult<UserRoadmapDto>> Generate(GenerateRoadmapRequestDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var gate = await _gateService.CheckAccessAsync(userId, Feature.RoadmapGeneration);
            if (!gate.Allowed)
            {
                return StatusCode(403, new
                {
                    code = "LIMIT_REACHED",
                    feature = "roadmap",
                    used = gate.Used,
                    limit = gate.Limit
                });
            }

            try
            {
                var result = await _userRoadmapService.GenerateRoadmapAsync(userId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating roadmap.", error = ex.Message });
            }
        }

        [HttpGet("my-roadmap")]
        //[Authorize]
        public async Task<ActionResult<UserRoadmapDto>> GetMyRoadmap()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var result = await _userRoadmapService.GetMyRoadmapAsync(userId);

            if (result is null)
                return NotFound(new { message = "No roadmap found. Generate one first via POST /api/roadmap/generate." });

            return Ok(result);
        }

        [HttpPost("rescan-gaps")]
        //[Authorize]
        public async Task<ActionResult<UserRoadmapDto>> RescanGaps()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var gate = await _gateService.CheckAccessAsync(userId, Feature.RoadmapRescan);
            if (!gate.Allowed)
            {
                return StatusCode(403, new
                {
                    code = "LIMIT_REACHED",
                    feature = "rescan",
                    used = gate.Used,
                    limit = gate.Limit
                });
            }

            try
            {
                var result = await _userRoadmapService.RescanGapAnalysisAsync(userId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while rescanning gaps.", error = ex.Message });
            }
        }
    }
}
