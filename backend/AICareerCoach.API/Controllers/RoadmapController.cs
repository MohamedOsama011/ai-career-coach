using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoadmapController : ControllerBase
    {
        private readonly AICareerCoachDbContext _context;

        public RoadmapController(AICareerCoachDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Roadmap>>> GetAll()
        {
            return await _context.Roadmaps.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Roadmap>> Add(Roadmap roadmap)
        {
            _context.Roadmaps.Add(roadmap);
            await _context.SaveChangesAsync();

            return Ok(roadmap);
        }
    }
}