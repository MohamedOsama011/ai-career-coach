using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.BLL.Interfaces.AI;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly IPdfReportService _pdfReportService;
        private readonly ICvFeedbackService _cvFeedbackService;
        private readonly IUserRoadmapService _userRoadmapService;
        private readonly UserManager<User> _userManager;

        public PdfController(
            IPdfReportService pdfReportService,
            ICvFeedbackService cvFeedbackService,
            IUserRoadmapService userRoadmapService,
            UserManager<User> userManager)
        {
            _pdfReportService = pdfReportService;
            _cvFeedbackService = cvFeedbackService;
            _userRoadmapService = userRoadmapService;
            _userManager = userManager;
        }

        [HttpGet("cv-report")]
        public async Task<IActionResult> GetCvReport()
        {
            var user = await _userManager.GetUserAsync(User);
            var feedback = await _cvFeedbackService.GetFeedbackAsync(user.Id);
            var pdf = _pdfReportService.GenerateCvReport(feedback);
            return File(pdf, "application/pdf", $"CV_Analysis_Report_{user.Id}.pdf");
        }

        [HttpGet("roadmap-report")]
        public async Task<IActionResult> GetRoadmapReport()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var userRoadmap = await _userRoadmapService.GetMyRoadmapAsync(userId);
            if (userRoadmap == null)
                return NotFound(new { message = "No roadmap found. Generate one first via POST /api/roadmap/generate." });

            var dto = new RoadmapDto
            {
                Track = userRoadmap.TemplateTrack,
                Title = userRoadmap.TargetRole,
                Description = userRoadmap.SeniorityGap ?? "Career progression roadmap",
                Steps = userRoadmap.Steps.Select(s => new RoadmapStepDto
                {
                    Title = s.Title,
                    Description = s.Description,
                    Level = s.Level,
                    Resources = s.Resources ?? new(),
                    OrderIndex = s.Order
                }).ToList()
            };

            var pdf = _pdfReportService.GenerateRoadmapReport(dto);
            return File(pdf, "application/pdf", $"Roadmap_Report_{userId}.pdf");
        }
    }
}
