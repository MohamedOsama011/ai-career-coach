using AICareerCoach.API.Response;
using AICareerCoach.BLL.DTO.User;
using AICareerCoach.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public UserController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<GeneralResponse>> GetAll()
        {
            var users = _userManager.Users.ToList();

            var data = users.Select(u => new Get
            {
                name = u.FullName,
                email = u.Email ?? string.Empty,
                title = u.CareerGoal
            }).ToList();

            return Ok(new GeneralResponse
            {
                Sucess = true,
                Data = data
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GeneralResponse>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound(new GeneralResponse
                {
                    Sucess = false,
                    Data = "user not found"
                });

            return Ok(new GeneralResponse
            {
                Sucess = true,
                Data = new Get
                {
                    name = user.FullName,
                    email = user.Email ?? string.Empty,
                    title = user.CareerGoal
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddUser([FromBody] Add user)
        {
            var user1 = new User
            {
                FullName = user.Name,
                Email = user.email,
                UserName = user.email
            };

            var result = await _userManager.CreateAsync(user1);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return CreatedAtAction(nameof(GetUser), new { id = user1.Id }, user1);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<GeneralResponse>> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new GeneralResponse
                {
                    Sucess = false,
                    Data = "not found"
                });
            }

            await _userManager.DeleteAsync(user);

            return Ok(new GeneralResponse
            {
                Sucess = true,
                Data = "deleted successfully"
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<GeneralResponse>> Edit([FromBody] Update user1, string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound(new GeneralResponse
                {
                    Sucess = false,
                    Data = "user not found"
                });
            }

            user.Email = user1.Email;
            user.UserName = user1.Email;
            user.FullName = user1.FullName;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new GeneralResponse
            {
                Sucess = true,
                Data = "user updated successfully"
            });
        }
    }
}
