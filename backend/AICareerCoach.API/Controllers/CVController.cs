using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CVController : ControllerBase
    {
        private readonly ICVService _cvService;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;

        public CVController(ICVService cvService, IConfiguration config, UserManager<User> userManager)
        {
            _cvService = cvService;
            _config = config;
            _userManager = userManager;
        }


        //[HttpPost("upload")]
        //public async Task<IActionResult> UploadCV(
        //    IFormFile file,
        //    string userId)
        //{
        //    if (file == null || file.Length == 0)
        //        return BadRequest("File is required");

        //    var cv =
        //        await _cvService.UploadCVAsync(
        //            file.OpenReadStream(),
        //            file.FileName,
        //            userId);

        //    var fileName = Path.GetFileName(cv.FilePath);

        //    return Ok(new
        //    {
        //        cv.CVId,
        //        cv.UserId,
        //        cv.UploadedAt,
        //        FileUrl = $"http://localhost:5068/cvs/{fileName}"
        //    });
        //}

        //[HttpGet("user/{userId}")]
        //public IActionResult GetUserCVs(string userId)
        //{
        //    var cvs = _cvService.GetUserCVs(userId);

        //    var result = cvs.Select(cv => new
        //    {
        //        cv.CVId,
        //        cv.UploadedAt,

        //        FileName = Path.GetFileName(cv.FilePath),

        //        DownloadUrl =
        //            $"{Request.Scheme}://{Request.Host}/cvs/{Path.GetFileName(cv.FilePath)}"
        //    });

        //    return Ok(result);
        //}

        [HttpPost("upload")]
        public async Task<IActionResult> UploadCV(IFormFile file, [FromQuery] string? userId = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var effectiveUserId = user?.Id ?? userId;

                if (string.IsNullOrEmpty(effectiveUserId))
                    return Unauthorized("User ID required");

                if (file == null || file.Length == 0)
                    return BadRequest("File is required");

                var cv = await _cvService.UploadCVAsync(
                    file.OpenReadStream(),
                    file.FileName,
                    effectiveUserId);

                var fileName = Path.GetFileName(cv.FilePath);
                var baseUrl = _config["AppSettings:BaseUrl"];

                return Ok(new CVResponseDto
                {
                    CVId = cv.CVId,
                    UserId = cv.UserId,
                    UploadedAt = cv.UploadedAt,
                    FileName = Path.GetFileName(cv.FilePath) ?? "Unknown.pdf",
                    DownloadUrl = $"{baseUrl}/cvs/{fileName}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
        [HttpGet("user/{userId}")]
        public IActionResult GetUserCVs(string userId)
        {
            var cvs = _cvService.GetUserCVs(userId);
            var baseUrl = _config["AppSettings:BaseUrl"];

            var result = cvs.Select(cv => new CVResponseDto
            {
                CVId = cv.CVId,
                UploadedAt = cv.UploadedAt,
                UserId = cv.UserId,
                FileName = Path.GetFileName(cv.FilePath),
                DownloadUrl = $"{baseUrl}/cvs/{Path.GetFileName(cv.FilePath)}"
            });

            return Ok(result);
        }

        [HttpDelete("{cvId}")]
public IActionResult DeleteCV(int cvId)
{
    try
    {
        _cvService.DeleteCV(cvId);
        return Ok(new { message = "CV deleted successfully" });
    }
    catch (Exception ex)
    {
        return NotFound(ex.Message);
    }
}

    }
}
