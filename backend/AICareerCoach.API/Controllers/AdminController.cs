using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.DTOs.Notification;
using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics()
        {
            return Ok(await _adminService.GetDashboardStatisticsAsync());
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            return Ok(await _adminService.GetAllUsersAsync());
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _adminService.DeleteUserAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleDto dto)
        {
            var result = await _adminService.ChangeUserRoleAsync(id, dto.Role);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpGet("cvs")]
        public async Task<IActionResult> GetCVs()
        {
            return Ok(await _adminService.GetAllCVsAsync());
        }

        [HttpDelete("cvs/{id}")]
        public async Task<IActionResult> DeleteCV(int id)
        {
            var result = await _adminService.DeleteCVAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpGet("cvs/{id}/download")]
        public async Task<IActionResult> DownloadCV(int id)
        {
            try
            {
                var file = await _adminService.DownloadCVAsync(id);
                if (file == null)
                    return NotFound();

                return PhysicalFile(file.FilePath, "application/pdf", file.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed to download CV", error = ex.Message });
            }
        }

        [HttpGet("user-management")]
        public async Task<IActionResult> GetUserManagement()
        {
            var result = await _adminService.GetUserManagementAsync();
            return Ok(result);
        }

        [HttpGet("sync-logs")]
        public async Task<IActionResult> GetSyncLogs([FromQuery] int count = 50)
        {
            var result = await _adminService.GetSyncLogsAsync(count);
            return Ok(result);
        }

        [HttpGet("users/{id}/detail")]
        public async Task<IActionResult> GetUserDetail(string id)
        {
            var result = await _adminService.GetUserDetailAsync(id);
            if (result == null)
                return NotFound(new { message = "User not found" });
            return Ok(result);
        }

        [HttpDelete("cache")]
        public async Task<IActionResult> ClearCache([FromQuery] int? userId)
        {
            await _adminService.ClearCacheAsync(userId);
            return Ok(new { message = userId.HasValue ? "Cache cleared for user" : "All cache cleared" });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var result = await _adminService.GetReportsAsync();
            return Ok(result);
        }

        [HttpGet("reports/export")]
        public async Task<IActionResult> ExportCsv([FromQuery] string type)
        {
            try
            {
                var bytes = await _adminService.ExportCsvAsync(type);
                var fileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                return File(bytes, "text/csv; charset=utf-8", fileName);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to export CSV", error = ex.Message });
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            var result = await _adminService.GetHealthAsync();
            return Ok(result);
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Body))
                return BadRequest(new { message = "Title and Body are required" });

            try
            {
                switch (dto.TargetType.ToLowerInvariant())
                {
                    case "all":
                        await _adminService.SendBroadcastToAllAsync(dto.Title, dto.Body, dto.Type);
                        break;
                    case "plan":
                        if (string.IsNullOrWhiteSpace(dto.TargetValue))
                            return BadRequest(new { message = "Plan name required for plan target" });
                        await _adminService.SendBroadcastToPlanAsync(dto.TargetValue, dto.Title, dto.Body, dto.Type);
                        break;
                    case "user":
                        if (string.IsNullOrWhiteSpace(dto.TargetValue))
                            return BadRequest(new { message = "User ID required for user target" });
                        await _adminService.SendBroadcastToUserAsync(dto.TargetValue, dto.Title, dto.Body, dto.Type);
                        break;
                    default:
                        return BadRequest(new { message = "Invalid target type. Use: all, plan, or user" });
                }

                return Ok(new { message = "Broadcast sent successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send broadcast", error = ex.Message });
            }
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? action = null,
            [FromQuery] string? adminId = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _adminService.GetAuditLogsAsync(page, pageSize, action, adminId);
            return Ok(result);
        }

        [HttpGet("chat-sessions")]
        public async Task<IActionResult> GetChatSessions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var result = await _adminService.GetChatSessionsAsync(page, pageSize);
            return Ok(result);
        }

        [HttpGet("chat-sessions/{sessionId:int}/messages")]
        public async Task<IActionResult> GetChatMessages(int sessionId)
        {
            var messages = await _adminService.GetChatMessagesAsync(sessionId);
            return Ok(messages);
        }
    }
}
