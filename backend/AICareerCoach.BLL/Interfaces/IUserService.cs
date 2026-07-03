using AICareerCoach.BLL.DTOs.User;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(string userId);
    }
}
