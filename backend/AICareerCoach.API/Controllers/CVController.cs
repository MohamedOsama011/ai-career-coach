using AICareerCoach.BLL.DTOs.CV;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AICareerCoach.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CVController : ControllerBase
    {
        private readonly ICVService _cvService;
        private readonly IConfiguration _config;
        private readonly UserManager<User> _userManager;
        private readonly AICareerCoachDbContext _context;

        public CVController(
            ICVService cvService,
            IConfiguration config,
            UserManager<User> userManager,
            AICareerCoachDbContext context)
        {
            _cvService = cvService;
            _config = config;
            _userManager = userManager;
            _context = context;
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
        public async Task<IActionResult> UploadCV(IFormFile file, [FromForm] string? userId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var effectiveUserId = user?.Id ?? userId;

            if (string.IsNullOrEmpty(effectiveUserId))
                return Unauthorized("User ID required. Either authenticate or provide userId in the form.");

            if (file == null || file.Length == 0)
                return BadRequest("File is required");
            
            var result = await _cvService.UploadCVAsync(
                file.OpenReadStream(),
                file.FileName,
                effectiveUserId);

            var fileName = Path.GetFileName(result.Cv.FilePath);
            var baseUrl = _config["AppSettings:BaseUrl"];

            return Ok(new CVResponseDto
            {
                CVId = result.Cv.CVId,
                UserId = result.Cv.UserId,
                UploadedAt = result.Cv.UploadedAt,
                FileName = Path.GetFileName(result.Cv.FilePath) ?? "Unknown.pdf",

                DownloadUrl = $"{baseUrl}/cvs/{fileName}",
                IsNew = result.IsNew
            });
        }

        [HttpGet("{cvId}/text")]
        [Authorize]
        public async Task<IActionResult> GetCvText(int cvId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var cv = await _context.Set<CV>()
                .FirstOrDefaultAsync(c => c.CVId == cvId && c.UserId == user.Id);
            if (cv == null)
                return NotFound(new { message = "CV not found." });

            return Ok(new { extractedData = cv.ExtractedData ?? string.Empty });
        }

        [HttpGet("my")]
        public IActionResult GetMyCVs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User authentication required." });

            var cvs = _cvService.GetUserCVs(userId);
            var baseUrl = _config["AppSettings:BaseUrl"];

            var result = cvs.Select(cv => new CVResponseDto
            {
                CVId = cv.CVId,
                UploadedAt = cv.UploadedAt,
                UserId = cv.UserId,
                FileName = Path.GetFileName(cv.FilePath),
                DownloadUrl = $"{baseUrl}/cvs/{Path.GetFileName(cv.FilePath)}",
                IsNew = false
            });

            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public IActionResult GetUserCVs(string userId)
        {
            var effectiveUserId = userId;
            if (string.IsNullOrEmpty(effectiveUserId))
                effectiveUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(effectiveUserId))
                return Unauthorized(new { message = "User ID is required." });

            var cvs = _cvService.GetUserCVs(effectiveUserId);
            var baseUrl = _config["AppSettings:BaseUrl"];

            var result = cvs.Select(cv => new CVResponseDto
            {
                CVId = cv.CVId,
                UploadedAt = cv.UploadedAt,
                UserId = cv.UserId,
                FileName = Path.GetFileName(cv.FilePath),
                DownloadUrl = $"{baseUrl}/cvs/{Path.GetFileName(cv.FilePath)}",
                IsNew = false
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
