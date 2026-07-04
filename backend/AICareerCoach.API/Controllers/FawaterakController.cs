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
            var result = await _fawaterakService.CreatePaymentAsync(dto);
            return Ok(result);
        }

        [HttpPost("execute-invoice")]
        public async Task<IActionResult> ExecuteInvoice(string methodId, string userSubscriptionId)
        {
            var result = await _fawaterakService.ExecuteInvoiceAsync(methodId, userSubscriptionId);
            return Ok(result);
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
