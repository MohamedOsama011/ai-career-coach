using System.Security.Claims;
using AICareerCoach.BLL.DTOs.Subscription;
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

        [HttpGet("my")]
        public async Task<IActionResult> GetMy()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userSubscriptionService.GetAllByUserIdAsync(userId);
            return Ok(result);
        }

        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _userSubscriptionService.GetAllAsync(search, from, to);
            return Ok(result);
        }

        [HttpGet("{id:int}/detail")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _userSubscriptionService.GetSubscriberDetailAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAnalytics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var result = await _userSubscriptionService.GetAnalyticsAsync(from, to);
            return Ok(result);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userSubscriptionService.GetStatusAsync(userId);
            return Ok(result);
        }

        [HttpGet("my/payments")]
        public async Task<IActionResult> GetPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userSubscriptionService.GetPaymentHistoryAsync(userId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("payments/{paymentId:int}/invoice")]
        public async Task<IActionResult> GetPaymentInvoice(int paymentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userSubscriptionService.GetPaymentInvoiceAsync(paymentId, userId);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _userSubscriptionService.CancelSubscriptionAsync(id, userId);
            if (!result.Success)
            {
                var msg = result.Data;
                if (msg == "subscription not found")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }
    }
}
