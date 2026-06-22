using AICareerCoach.BLL.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly IPdfReportService _pdfService;

        public ReportsController(IPdfReportService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpGet("cv")]
        public IActionResult DownloadCvReport()
        {
            var pdf = _pdfService.GenerateCvReport(
                "Test User",
                "Strong CV with good backend skills in .NET and SQL"
            );

            return File(pdf, "application/pdf", "CV_Report.pdf");
        }

        [HttpGet("roadmap")]
        public IActionResult DownloadRoadmapReport()
        {
            var pdf = _pdfService.GenerateRoadmapReport(
                "Test User",
                "Step 1: Learn C#\nStep 2: ASP.NET Core\nStep 3: Projects"
            );

            return File(pdf, "application/pdf", "Roadmap_Report.pdf");
        }

        [HttpGet("interview")]
        public IActionResult DownloadInterviewReport()
        {
            var pdf = _pdfService.GenerateInterviewReport(
                "Test User",
                "Good communication skills but needs improvement in system design"
            );

            return File(pdf, "application/pdf", "Interview_Report.pdf");
        }
    }
}