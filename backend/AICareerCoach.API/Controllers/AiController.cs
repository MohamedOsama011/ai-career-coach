using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly ICvFeedbackService _feedbackService;
        private readonly UserManager<User> _userManager;
        private readonly IPdfReportService _pdfReportService;

        public AiController(
            ICvFeedbackService feedbackService,
            UserManager<User> userManager,
            IPdfReportService pdfReportService)
        {
            _feedbackService = feedbackService;
            _userManager = userManager;
            _pdfReportService = pdfReportService;
        }

        [HttpGet("cv-feedback")]
        public async Task<IActionResult> GetCvFeedback()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                    return Unauthorized();

                var result = await _feedbackService.GetFeedbackAsync(user.Id);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("cv-feedback-report")]
        public async Task<IActionResult> DownloadCvReport([FromQuery] string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return BadRequest("UserId is required.");

                var feedback = await _feedbackService.GetFeedbackAsync(userId);

                var pdf = _pdfReportService.GenerateCvAnalysisReport(feedback);

                return File(
                    pdf,
                    "application/pdf",
                    $"CV_Analysis_Report_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }
        }
    }
}