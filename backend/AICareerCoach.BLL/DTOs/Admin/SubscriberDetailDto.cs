using AICareerCoach.BLL.DTOs.Subscription;

namespace AICareerCoach.BLL.DTOs.Admin
{
    public class SubscriberDetailDto
    {
        public SubscriberUserDetail User { get; set; } = new();
        public SubscriptionDetail Subscription { get; set; } = new();
        public List<PaymentInvoiceDto> RecentPayments { get; set; } = new();
        public List<AuditLogDto> AuditLogEntries { get; set; } = new();
        public List<SubscriberSessionDto> RecentSessions { get; set; } = new();
        public List<SubscriberCvDto> CVs { get; set; } = new();
        public List<SubscriberRoadmapDto> Roadmaps { get; set; } = new();
    }

    public class SubscriberUserDetail
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateTime JoinDate { get; set; }
        public int CvCount { get; set; }
    }

    public class SubscriptionDetail
    {
        public int Id { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? DaysRemaining { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "EGP";
    }

    public class AuditLogDto
    {
        public int Id { get; set; }
        public string AdminUserId { get; set; } = string.Empty;
        public string AdminUserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int? UserSubscriptionId { get; set; }
        public string? TargetUserId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubscriberSessionDto
    {
        public int Id { get; set; }
        public string Track { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int QuestionsAsked { get; set; }
        public int MaxQuestions { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubscriberCvDto
    {
        public int CvId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }
    }

    public class SubscriberRoadmapDto
    {
        public int Id { get; set; }
        public string TargetRole { get; set; } = string.Empty;
        public string TemplateTrack { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
