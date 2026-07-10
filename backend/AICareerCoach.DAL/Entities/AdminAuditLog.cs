using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    public class AdminAuditLog
    {
        public int Id { get; set; }
        public string? AdminUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public virtual User? AdminUser { get; set; }
    }
}
