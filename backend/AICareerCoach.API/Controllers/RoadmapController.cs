using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoadmapController : ControllerBase
    {
        private readonly IRoadmapService _roadmapService;

        public RoadmapController(IRoadmapService roadmapService) => _roadmapService = roadmapService;

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
    }
}
