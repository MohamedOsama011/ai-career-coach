using AICareerCoach.BLL.services.cv;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CVController : ControllerBase
    {
        private readonly ICVService _cvService;

        public CVController(ICVService cvService)
        {
            _cvService = cvService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadCV(
            IFormFile file,
            string userId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required");

            var cv =
                await _cvService.UploadCVAsync(
                    file.OpenReadStream(),
                    file.FileName,
                    userId);

            return Ok(cv);
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetUserCVs(string userId)
        {
            var cvs =
                _cvService.GetUserCVs(userId);

            return Ok(cvs);
        }

        [HttpDelete("{cvId}")]
        public IActionResult DeleteCV(int cvId)
        {
             _cvService.DeleteCV(cvId);

            return Ok("CV Deleted Successfully");
        }
    }
}
