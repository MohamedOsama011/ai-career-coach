using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserSubscriptionController : ControllerBase
    {
        private readonly IUserSubscriptionService _userSubscriptionService;

        public UserSubscriptionController(IUserSubscriptionService userSubscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
        }

        [HttpGet("by-user/{userId}")]
        public async Task<IActionResult> GetByUser(string userId)
        {
            var result = await _userSubscriptionService.GetAllByUserIdAsync(userId);
            return Ok(result);
        }
    }
}
