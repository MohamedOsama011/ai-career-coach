using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly ICvFeedbackService _feedbackService;
        private readonly ILlmService _llmService;
        private readonly UserManager<User> _userManager;

        public AiController(ICvFeedbackService feedbackService, UserManager<User> userManager, ILlmService llmService)
        {
            _feedbackService = feedbackService;
            _userManager = userManager;
            _llmService = llmService;
        }

        [HttpGet("cv-feedback")]
        public async Task<IActionResult> GetCvFeedback(IFormFile File)
        {
            var stream = File.OpenReadStream();
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var result = await _llmService.GetCvFeedbackAsync(stream,user.Id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
