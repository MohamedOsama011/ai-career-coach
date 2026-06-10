using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Auth;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Http;
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

        [HttpPost("register")] // api/auth/register
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

        [HttpPost("login")] // api/auth/login
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
		[HttpPost("Addnewrole")]
		public async Task<object> Addrole([FromBody]string role)
		{
			
				var result = await _authService.addrole(role);
			return result;
		}
		[HttpPost("AssignRole")]
		public async Task<Object> assignrole([FromBody] Role role)
		{
			
				var result = await _authService.Sign_IN_role(role);
				return result;
			
		}
		[HttpPost("refreshtoken")]
		public async Task<object> Refreshtoken([FromBody] Refreshtokendto refreshtokendto)
		{
			
				var result = await _authService.RefreshTocken(refreshtokendto);
			
			return result;
				
		}
		[HttpPost("logout")]
		public async Task Logout([FromBody] Refreshtokendto token)
		{
			
				 await _authService.Logout(token);
				
			
		}
		[HttpPost("logoutall/{id:int}")]
		public async Task Logoutall(int id )
		{
			await _authService.Logoutall(id);
		}
		[HttpPost("ForgotPassword")]
		public async Task ForgotPasswrd(ForgotPassword forgotPassword)
		{
			await _authService.ForgotPassword(forgotPassword);
		}

		[HttpPost("ResetPassword")]
		public async Task ResetPassword(ResetPassword resetPassword)
		{
			await _authService.ResetPassword(resetPassword);
		}

		[HttpPost("changepassword")]
		public async Task<Object> changepassword(CangePassword cangePassword)
		{
			var user = await _userManager.GetUserAsync(User);
		var result=	await _authService.changepassword(user, cangePassword);
			return result;

		}


	}
}
