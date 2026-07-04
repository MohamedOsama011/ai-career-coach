using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Fawaterak;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IFawaterakService
    {
        Task<CreatePaymentResponseDto> CreatePaymentAsync(CreatePaymentRequestDto dto);
        Task<object> ExecuteInvoiceAsync(string methodId, string userSubscriptionId);
        Task<GetPaymentMethodsResponseDto> GetPaymentMethodsAsync();
        Task<ExecutePaymentResponseDto> ExecutePaymentAsync(FawaterakPaymentRequestDto request);
        Task<GetTransactionResponseDto> GetTransactionDataAsync(GetTransactionRequestDto dto);
        Task<Generalresponse> HandleSuccessWebhookAsync(WebhookSuccessDto dto);
    }
}
