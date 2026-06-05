using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CVController : ControllerBase
    {
        private readonly AICareerCoachDbContext _context;

        public CVController(AICareerCoachDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CV>>> GetAll()
        {
            return await _context.CVs.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<CV>> Add(CV cv)
        {
            _context.CVs.Add(cv);
            await _context.SaveChangesAsync();

            return Ok(cv);
        }
    }
}