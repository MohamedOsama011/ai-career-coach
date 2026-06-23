using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InterviewController : ControllerBase
    {
        private readonly IInterviewService _interviewService;

        public InterviewController(IInterviewService interviewService)
        {
            _interviewService = interviewService;
        }

        [HttpGet("options")]
        public async Task<ActionResult<InterviewOptionsDto>> GetOptions()
        {
            var result = await _interviewService.GetOptionsAsync();
            return Ok(result);
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<InterviewSessionDto>> StartSession([FromBody] StartSessionRequestDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _interviewService.StartSessionAsync(userId, dto);
                return CreatedAtAction(nameof(GetActiveSession), null, result);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("sessions/active")]
        public async Task<ActionResult<InterviewSessionDto>> GetActiveSession()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var result = await _interviewService.GetActiveSessionAsync(userId);

            if (result is null)
                return NotFound(new { message = "No active interview session found." });

            return Ok(result);
        }

        [HttpPost("sessions/{sessionId}/answers")]
        public async Task<ActionResult<InterviewSessionDto>> SubmitAnswer(int sessionId, [FromBody] SubmitAnswerRequestDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _interviewService.SubmitAnswerAsync(userId, sessionId, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("sessions/{sessionId}/scorecard")]
        public async Task<ActionResult<InterviewScorecardDto>> GetScorecard(int sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _interviewService.GetScorecardAsync(userId, sessionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<InterviewHistoryItemDto>>> GetHistory()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var result = await _interviewService.GetHistoryAsync(userId);
            return Ok(result);
        }
    }
}
