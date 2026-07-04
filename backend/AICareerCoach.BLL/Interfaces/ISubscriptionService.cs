using AICareerCoach.BLL.DTOs;
using AICareerCoach.DAL.Entities;

namespace AICareerCoach.BLL.Interfaces
{
    public interface ISubscriptionService
    {
        Task<Generalresponse> GetAllSubscriptionsAsync();
        Task<Generalresponse> GetSubscriptionByIdAsync(string id);
        Task CreateSubscriptionAsync(SubscriptionDto subscription);
        Task DeleteSubscriptionAsync(Subscription subscription);
        Task<Generalresponse> UpdateSubscriptionAsync(SubscriptionDto dto, string id);
    }
}
