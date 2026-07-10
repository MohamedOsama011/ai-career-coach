using AICareerCoach.BLL.DTOs.Notification;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(AICareerCoachDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendToUserAsync(string userId, string title, string body, string type)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation("Notification sent to user {UserId}: {Title}", userId, title);
        }

        public async Task SendToAllAsync(string title, string body, string type)
        {
            var userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Broadcast sent to all {Count} users: {Title}", userIds.Count, title);
        }

        public async Task SendToPlanAsync(string planName, string title, string body, string type)
        {
            var userIds = await _context.UserSubscriptions
                .Where(us => us.Subscription != null && us.Subscription.Name == planName && us.IsActive && us.EndDate > DateTime.UtcNow)
                .Select(us => us.UserId)
                .Distinct()
                .ToListAsync();

            if (userIds.Count == 0)
            {
                _logger.LogWarning("No active subscribers found for plan {PlanName}", planName);
                return;
            }

            var notifications = userIds.Select(userId => new Notification
            {
                UserId = userId,
                Title = title,
                Body = body,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Broadcast sent to {Count} {PlanName} subscribers: {Title}", userIds.Count, planName, title);
        }

        public async Task<PaginatedNotificationsDto> GetUserNotificationsAsync(string userId, int page, int pageSize)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();
            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Body = n.Body,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TimeAgo = FormatTimeAgo(n.CreatedAt),
                })
                .ToListAsync();

            return new PaginatedNotificationsDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                UnreadCount = unreadCount,
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(string userId)
        {
            var count = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(setters => setters.SetProperty(n => n.IsRead, true));

            return count;
        }

        private static string FormatTimeAgo(DateTime dateTime)
        {
            var diff = DateTime.UtcNow - dateTime;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return dateTime.ToString("MMM d");
        }
    }
}
