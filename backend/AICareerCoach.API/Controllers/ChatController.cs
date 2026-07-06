using AICareerCoach.BLL.DTOs.Chat;
using AICareerCoach.BLL.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatAssistantService _chatAssistantService;

        public ChatController(IChatAssistantService chatAssistantService)
        {
            _chatAssistantService = chatAssistantService;
        }

        [HttpPost("sessions")]
        public async Task<ActionResult<ChatSessionDto>> CreateSession()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var result = await _chatAssistantService.CreateSessionAsync(userId);
            return CreatedAtAction(nameof(GetSession), new { sessionId = result.Id }, result);
        }

        [HttpGet("sessions")]
        public async Task<ActionResult<List<ChatSessionSummaryDto>>> GetUserSessions()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            var result = await _chatAssistantService.GetUserSessionsAsync(userId);
            return Ok(result);
        }

        [HttpGet("sessions/{sessionId:int}")]
        public async Task<ActionResult<ChatSessionDto>> GetSession(int sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _chatAssistantService.GetSessionAsync(userId, sessionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while loading the chat session.", error = ex.Message });
            }
        }

        [HttpPost("sessions/{sessionId:int}/messages")]
        public async Task<ActionResult<ChatSessionDto>> SendMessage(int sessionId, [FromBody] SendChatMessageDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var result = await _chatAssistantService.SendMessageAsync(userId, sessionId, dto.Message);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while sending the chat message.", error = ex.Message });
            }
        }
    }
}
