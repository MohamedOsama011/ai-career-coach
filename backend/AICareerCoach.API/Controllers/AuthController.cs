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
        private readonly UserManager<User> _userManager;

        public AuthController(IAuthService authService, UserManager<User> userManager)
        {
            _authService = authService;
            _userManager = userManager;
        }

        [HttpPost("register")]
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
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPassword forgotPassword)
        {
            await _authService.ForgotPassword(forgotPassword);
            return Ok();
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword resetPassword)
        {
            var result = await _authService.ResetPassword(resetPassword);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("changepassword")]
        public async Task<IActionResult> changepassword([FromBody] CangePassword cangePassword)
        {
            var user = await _userManager.GetUserAsync(User);
            var result = await _authService.changepassword(user!, cangePassword);
            return Ok(result);
        }
    }
}
