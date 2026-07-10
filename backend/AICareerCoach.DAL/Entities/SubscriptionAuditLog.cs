using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    public class SubscriptionAuditLog
    {
        public int Id { get; set; }
        public string? AdminUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public int? UserSubscriptionId { get; set; }
        public string? TargetUserId { get; set; }
        public string? PreviousValues { get; set; }
        public string? NewValues { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User? AdminUser { get; set; }
        public virtual UserSubscription? UserSubscription { get; set; }
    }
}
