using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InterviewController : ControllerBase
    {
        private readonly AICareerCoachDbContext _context;

        public InterviewController(AICareerCoachDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Interview>>> GetAll()
        {
            return await _context.Interviews.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Interview>> Add(Interview interview)
        {
            _context.Interviews.Add(interview);
            await _context.SaveChangesAsync();

            return Ok(interview);
        }
    }
}