using System.Data;
using System.Security.Claims;
using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Auth;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;

        public AuthController(IAuthService authService, IUserService userService, UserManager<User> userManager)
        {
            _authService = authService;
            _userService = userService;
            _userManager = userManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [Authorize(Roles ="Admin")]

        [HttpPost("Addnewrole")]
        public async Task<IActionResult> Addrole([FromBody] string role)
        {
            var result = await _authService.addrole(role);
            return Ok(result);
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> assignrole([FromBody] Role role)
        {
            var result = await _authService.Sign_IN_role(role);
            return Ok(result);
        }

        [HttpPost("refreshtoken")]
        [AllowAnonymous]
        public async Task<IActionResult> Refreshtoken([FromBody] Refreshtokendto refreshtokendto)
        {
            var result = await _authService.RefreshTocken(refreshtokendto);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] Refreshtokendto token)
        {
            await _authService.Logout(token);
            return Ok();
        }

        [HttpPost("logoutall/{id:int}")]
        public async Task<IActionResult> Logoutall(int id)
        {
            await _authService.Logoutall(id);
            return Ok();
        }

        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword forgotPassword)
        {
            await _authService.ForgotPassword(forgotPassword);
            return Ok();
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword resetPassword)
        {
            var result = await _authService.ResetPassword(resetPassword);
            return Ok(result);
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User identity could not be verified from the token." });

            try
            {
                var profile = await _userService.GetProfileAsync(userId);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("changepassword")]
        public async Task<IActionResult> changepassword([FromBody] CangePassword cangePassword)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _authService.changepassword(user!, cangePassword);
            return Ok(result);
        }
		[Authorize(Roles ="Admin")]
		[HttpPost("GetSystemRoles")]
		public async Task<IActionResult> GetSystemRoles()
		{
			var result = await _authService.Getsystemroles();
			return Ok(result);
		}

		[Authorize]
        [HttpPost("GetUserRoles")]
		public async Task<IActionResult> GetUserRoles()
		{
			var user = await _userManager.GetUserAsync(User);
            var result = await _authService.Getuserroles(user!);
			return Ok(result);
		}
		[Authorize(Roles ="Admin")]
        [HttpPost("ChangeUserRole/{id:alpha}")]
		public async Task<IActionResult> ChangeUserRole(string id,[FromBody]string role )
		{
            var user = await _userManager.FindByIdAsync(id);
			
			var result = await _authService.Changeuserrole(user!, role);
			return Ok(result);
		}
	}
}
