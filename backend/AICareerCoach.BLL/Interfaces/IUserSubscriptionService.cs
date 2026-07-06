using AICareerCoach.BLL.DTOs;
using AICareerCoach.BLL.DTOs.Admin;
using AICareerCoach.BLL.DTOs.Subscription;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IUserSubscriptionService
    {
        Task<GeneralResponse<List<UserSubscription>>> GetAllByUserIdAsync(string userId);
        Task<GeneralResponse<List<UserSubscription>>> GetAllAsync(string? search = null, DateTime? from = null, DateTime? to = null);
        Task<GeneralResponse<string>> CancelSubscriptionAsync(int id, string userId);
        Task<SubscriptionGateStatusDto> GetStatusAsync(string userId);
        Task<int> RefreshExpiredSubscriptionsAsync(string userId);
        Task<GeneralResponse<PagedPaymentHistoryDto>> GetPaymentHistoryAsync(string userId, int page, int pageSize);
        Task<GeneralResponse<PaymentInvoiceDto>> GetPaymentInvoiceAsync(int paymentId, string userId);
        Task<RevenueAnalyticsDto> GetAnalyticsAsync(DateTime? fromDate, DateTime? toDate);
        Task<GeneralResponse<SubscriberDetailDto>> GetSubscriberDetailAsync(int id);
    }
}
