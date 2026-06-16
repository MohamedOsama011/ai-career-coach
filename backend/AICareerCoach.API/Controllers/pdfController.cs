using Microsoft.AspNetCore.Mvc;
using AICareerCoach.BLL.Services.Interfaces;

namespace AICareerCoach.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PdfController : ControllerBase
    {
        private readonly IPdfExtractorService _pdfExtractorService;

        public PdfController(IPdfExtractorService pdfExtractorService)
        {
            _pdfExtractorService = pdfExtractorService;
        }

        [HttpPost("extract")]
        public async Task<IActionResult> ExtractPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a PDF file.");

            if (file.ContentType != "application/pdf")
                return BadRequest("Only PDF files are allowed.");

            try
            {
                var text = await _pdfExtractorService.ExtractTextAsync(file.OpenReadStream());

                return Ok(new
                {
                    extractedText = text,
                    length = text?.Length ?? 0,
                    isEmpty = string.IsNullOrWhiteSpace(text)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to extract PDF text",
                    error = ex.Message
                });
            }
        }
    }
}