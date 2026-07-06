using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Admin;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IAdminSubscriptionService
    {
        Task<GeneralResponse<SubscriberDetailDto>> GetSubscriberDetailAsync(int id);
        Task<GeneralResponse<string>> ActivateSubscriptionAsync(int subscriptionId, string notes, string adminUserId);
        Task<GeneralResponse<string>> CancelSubscriptionAsync(int subscriptionId, string notes, bool immediate, string adminUserId);
        Task<GeneralResponse<string>> ExtendSubscriptionAsync(int subscriptionId, int additionalDays, string notes, string adminUserId);
        Task<GeneralResponse<string>> MarkPaymentPaidAsync(int paymentId, string notes, string adminUserId);
        Task<GeneralResponse<string>> RefundPaymentAsync(int paymentId, string notes, string adminUserId);
        Task<GeneralResponse<List<AuditLogDto>>> GetAuditLogAsync(int subscriptionId);
    }
}
