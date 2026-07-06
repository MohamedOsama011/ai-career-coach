using System.Security.Claims;
using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/admin/subscriptions")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminSubscriptionController : ControllerBase
    {
        private readonly IAdminSubscriptionService _adminSubscriptionService;

        public AdminSubscriptionController(IAdminSubscriptionService adminSubscriptionService)
        {
            _adminSubscriptionService = adminSubscriptionService;
        }

        [HttpGet("{id:int}/detail")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var result = await _adminSubscriptionService.GetSubscriberDetailAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id, [FromBody] AdminActionRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _adminSubscriptionService.ActivateSubscriptionAsync(id, request.Notes ?? "", adminId);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] AdminCancelRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _adminSubscriptionService.CancelSubscriptionAsync(id, request.Notes ?? "", request.Immediate, adminId);
            if (!result.Success)
            {
                if (result.Data == "subscription not found")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("{id:int}/extend")]
        public async Task<IActionResult> Extend(int id, [FromBody] ExtendSubscriptionRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _adminSubscriptionService.ExtendSubscriptionAsync(id, request.AdditionalDays, request.Notes ?? "", adminId);
            if (!result.Success)
            {
                if (result.Data == "subscription not found")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("~/api/admin/payments/{paymentId:int}/mark-paid")]
        public async Task<IActionResult> MarkPaymentPaid(int paymentId, [FromBody] AdminActionRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _adminSubscriptionService.MarkPaymentPaidAsync(paymentId, request.Notes ?? "", adminId);
            if (!result.Success)
            {
                if (result.Data == "payment not found")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpPost("~/api/admin/payments/{paymentId:int}/refund")]
        public async Task<IActionResult> Refund(int paymentId, [FromBody] AdminActionRequest request)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var result = await _adminSubscriptionService.RefundPaymentAsync(paymentId, request.Notes ?? "", adminId);
            if (!result.Success)
            {
                if (result.Data == "payment not found")
                    return NotFound(result);
                return BadRequest(result);
            }
            return Ok(result);
        }

        [HttpGet("{id:int}/audit-log")]
        public async Task<IActionResult> GetAuditLog(int id)
        {
            var result = await _adminSubscriptionService.GetAuditLogAsync(id);
            if (!result.Success)
                return NotFound(result);
            return Ok(result);
        }
    }

    public class AdminActionRequest
    {
        public string? Notes { get; set; }
    }

    public class AdminCancelRequest
    {
        public string? Notes { get; set; }
        public bool Immediate { get; set; }
    }
}
