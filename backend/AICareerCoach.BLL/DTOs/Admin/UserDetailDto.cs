using AICareerCoach.BLL.DTOs.Subscription;

namespace AICareerCoach.BLL.DTOs.Admin
{
    public class UserDetailDto
    {
        public UserInfoDto User { get; set; } = new();
        public List<SubscriberCvDto> CVs { get; set; } = new();
        public UserInterviewInfo Interviews { get; set; } = new();
        public List<SubscriberRoadmapDto> Roadmaps { get; set; } = new();
        public List<PaymentInvoiceDto> Payments { get; set; } = new();
    }

    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = "User";
        public string? CareerGoal { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserInterviewInfo
    {
        public int TotalCount { get; set; }
        public List<SubscriberSessionDto> RecentSessions { get; set; } = new();
    }
}
