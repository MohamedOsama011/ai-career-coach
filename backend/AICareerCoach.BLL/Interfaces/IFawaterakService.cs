using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IFawaterakService
    {
        Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto dto, string userId);
        Task<ExecutePaymentResponseDto> ExecuteInvoiceAsync(string methodId, string userSubscriptionId, string userId);
        Task<GetPaymentMethodsResponseDto> GetPaymentMethodsAsync();
        Task<ExecutePaymentResponseDto> ExecutePaymentAsync(FawaterakPaymentRequestDto request);
        Task<GetTransactionResponseDto> GetTransactionDataAsync(GetTransactionRequestDto dto);
        Task<GeneralResponse<WebhookSuccessDto>> HandleSuccessWebhookAsync(WebhookSuccessDto dto);
        Task<GeneralResponse<string>> ConfirmPaymentAsync(string userId);
    }
}
