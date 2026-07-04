using AICareerCoach.BLL.DTOs;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IUserSubscriptionService
    {
        Task<Generalresponse> GetAllByUserIdAsync(string userId);
    }
}
