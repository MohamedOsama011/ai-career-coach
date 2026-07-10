using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/admin/interviews")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminInterviewController : ControllerBase
    {
        private readonly IAdminInterviewService _adminInterviewService;

        public AdminInterviewController(IAdminInterviewService adminInterviewService)
        {
            _adminInterviewService = adminInterviewService;
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? track = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var result = await _adminInterviewService.GetSessionsAsync(
                    page, pageSize, status, track, difficulty, from, to);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to retrieve interview sessions", error = ex.Message });
            }
        }

        [HttpDelete("sessions/{sessionId:int}")]
        public async Task<IActionResult> DeleteSession(int sessionId)
        {
            try
            {
                var result = await _adminInterviewService.DeleteSessionAsync(sessionId);
                if (!result)
                    return NotFound(new { message = "Session not found" });
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete session", error = ex.Message });
            }
        }

        [HttpPost("sessions/{sessionId:int}/abort")]
        public async Task<IActionResult> AbortSession(int sessionId)
        {
            try
            {
                var result = await _adminInterviewService.AbortSessionAsync(sessionId);
                if (!result)
                    return NotFound(new { message = "Session not found or not active" });
                return Ok(new { message = "Session aborted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to abort session", error = ex.Message });
            }
        }
    }
}
