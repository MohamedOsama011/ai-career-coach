using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobController : ControllerBase
    {
        private readonly AICareerCoachDbContext _context;

        public JobController(AICareerCoachDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Job>>> GetAll()
        {
            return await _context.Jobs.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Job>> Add(Job job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            return Ok(job);
        }
    }
}