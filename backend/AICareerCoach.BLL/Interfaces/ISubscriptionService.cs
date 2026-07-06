using AICareerCoach.BLL.DTOs;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces
{
    public interface ISubscriptionService
    {
        Task<GeneralResponse<List<Subscription>>> GetAllSubscriptionsAsync();
        Task<GeneralResponse<Subscription>> GetSubscriptionByIdAsync(string id);
        Task CreateSubscriptionAsync(SubscriptionDto subscription);
        Task<GeneralResponse<string>> DeleteSubscriptionAsync(string id);
        Task<GeneralResponse<string>> UpdateSubscriptionAsync(SubscriptionDto dto, string id);
    }
}
