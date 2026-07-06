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
        public async Task<IActionResult> ChangeRole(
            string id,
            [FromBody] string role)
        {
            var result =
                await _adminService.ChangeUserRoleAsync(id, role);

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

        //[HttpGet("cvs/{id}/download")]
        //public async Task<IActionResult> DownloadCV(int id)
        //{
        //    var file = await _adminService.DownloadCVAsync(id);

        //    if (file == null)
        //        return NotFound();

        //    return PhysicalFile(
        //        file.FilePath,
        //        "application/pdf",
        //        file.FileName);
        //}

        [HttpGet("cvs/{id}/download")]
        public async Task<IActionResult> DownloadCV(int id)
        {
            try
            {
                var file = await _adminService.DownloadCVAsync(id);

                if (file == null)
                    return NotFound();

                return PhysicalFile(
                    file.FilePath,
                    "application/pdf",
                    file.FileName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("user-management")]
        public async Task<IActionResult> GetUserManagement()
        {
            var result = await _adminService.GetUserManagement();

            return Ok(result);
        }
    }
}