using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.DTOs.Roadmap;
using AICareerCoach.BLL.Exceptions;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InterviewController : ControllerBase
    {
        private readonly IInterviewService _interviewService;

        /// <summary>
        /// Cached serializer options for SSE events. SSE responses are written
        /// manually via <see cref="JsonSerializer.Serialize(object, JsonSerializerOptions)"/>
        /// (bypassing ASP.NET Core's output formatter), so we must set the
        /// camelCase naming policy explicitly to match the frontend's
        /// <c>InterviewStreamEvent</c> type. Without this, the wire format is
        /// PascalCase (<c>{"Type":"token",...}</c>) and the frontend's
        /// <c>parsed.type</c> / <c>parsed.content</c> checks silently miss every
        /// event.
        /// </summary>
        private static readonly JsonSerializerOptions SseJsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
        public async Task<IActionResult> SubmitAnswer(int sessionId, [FromBody] SubmitAnswerRequestDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var accept = Request.Headers.Accept.ToString();
            if (accept.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
            {
                return await StreamSubmitAnswerAsync(userId, sessionId, dto);
            }

            try
            {
                var result = await _interviewService.SubmitAnswerAsync(userId, sessionId, dto);
                return Ok(result);
            }
            catch (ConflictException ex)
            {
                return Conflict(new { message = ex.Message });
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

        private async Task<IActionResult> StreamSubmitAnswerAsync(string userId, int sessionId, SubmitAnswerRequestDto dto)
        {
            // Critical: disable ASP.NET Core's response buffering so SSE tokens
            // reach the client as they're produced. Without this, the entire
            // response is buffered until the LLM stream completes, which makes
            // the UI show "AI is thinking" for the full LLM generation time
            // instead of streaming token-by-token.
            var bodyFeature = HttpContext.Features.Get<IHttpResponseBodyFeature>();
            bodyFeature?.DisableBuffering();

            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";
            Response.Headers["X-Accel-Buffering"] = "no";

            // Send an SSE comment line as the very first write. This forces the
            // response headers + body to flush immediately, so the client knows
            // the stream is alive even before the first real token arrives.
            // Some browsers won't render streaming output until they see data.
            await Response.WriteAsync(": stream-open\n\n", HttpContext.RequestAborted);
            await Response.Body.FlushAsync(HttpContext.RequestAborted);

            try
            {
                await foreach (var token in _interviewService.SubmitAnswerStreamAsync(userId, sessionId, dto, HttpContext.RequestAborted))
                {
                    var json = JsonSerializer.Serialize(token, SseJsonOptions);
                    await Response.WriteAsync($"data: {json}\n\n", HttpContext.RequestAborted);
                    await Response.Body.FlushAsync(HttpContext.RequestAborted);
                }
            }
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
            }
            catch (ConflictException ex)
            {
                await WriteSseErrorAsync(ex.Message, "fatal");
            }
            catch (KeyNotFoundException ex)
            {
                await WriteSseErrorAsync(ex.Message, "fatal");
            }
            catch (InvalidOperationException ex)
            {
                await WriteSseErrorAsync(ex.Message, "fatal");
            }
            catch (Exception ex)
            {
                await WriteSseErrorAsync(ex.Message, "fatal");
            }

            return new EmptyResult();
        }

        private async Task WriteSseErrorAsync(string message, string code)
        {
            try
            {
                var errorEvent = new StreamTokenDto { Type = "error", Code = code, Message = message };
                var doneEvent = new StreamTokenDto { Type = "done" };
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorEvent, SseJsonOptions)}\n\n", HttpContext.RequestAborted);
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(doneEvent, SseJsonOptions)}\n\n", HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }
            catch
            {
            }
        }

        [HttpPost("sessions/{sessionId}/hint")]
        public async Task<ActionResult<HintResponseDto>> GetHint(int sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _interviewService.GetHintAsync(userId, sessionId);
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

        [HttpPost("sessions/{sessionId}/convert-to-roadmap")]
        public async Task<ActionResult<UserRoadmapDto>> ConvertToRoadmap(int sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _interviewService.ConvertScorecardToRoadmapAsync(userId, sessionId);
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while converting the scorecard.", error = ex.Message });
            }
        }

        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                await _interviewService.DeleteSessionAsync(userId, sessionId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the session.", error = ex.Message });
            }
        }
    }
}
