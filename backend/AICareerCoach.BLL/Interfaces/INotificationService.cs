using AICareerCoach.BLL.DTOs.Notification;

namespace AICareerCoach.BLL.Interfaces
{
    public interface INotificationService
    {
        Task SendToUserAsync(string userId, string title, string body, string type);
        Task SendToAllAsync(string title, string body, string type);
        Task SendToPlanAsync(string planName, string title, string body, string type);
        Task<PaginatedNotificationsDto> GetUserNotificationsAsync(string userId, int page, int pageSize);
        Task<int> GetUnreadCountAsync(string userId);
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
        Task<int> MarkAllAsReadAsync(string userId);
    }
}
