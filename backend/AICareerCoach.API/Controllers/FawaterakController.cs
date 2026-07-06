using System.Security.Claims;
using AICareerCoach.BLL.DTOs.Fawaterak;
using AICareerCoach.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FawaterakController : ControllerBase
    {
        private readonly IFawaterakService _fawaterakService;

        public FawaterakController(IFawaterakService fawaterakService)
        {
            _fawaterakService = fawaterakService;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _fawaterakService.CreatePaymentAsync(dto, userId);
            return Ok(result);
        }

        [HttpPost("execute-invoice")]
        public async Task<IActionResult> ExecuteInvoice(string methodId, string userSubscriptionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _fawaterakService.ExecuteInvoiceAsync(methodId, userSubscriptionId, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost("get-transaction-data")]
        public async Task<IActionResult> GetTransactionData([FromBody] GetTransactionRequestDto dto)
        {
            var result = await _fawaterakService.GetTransactionDataAsync(dto);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("success-webhook")]
        public async Task<IActionResult> SuccessWebhook([FromBody] WebhookSuccessDto dto)
        {
            var result = await _fawaterakService.HandleSuccessWebhookAsync(dto);
            return Ok(result);
        }
    }
}
