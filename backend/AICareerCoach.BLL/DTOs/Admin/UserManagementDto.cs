namespace AICareerCoach.BLL.DTOs.Admin
{
    public class UserManagementDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? CareerGoal { get; set; }
        public bool HasCv { get; set; }
        public int InterviewsCount { get; set; }
        public string Plan { get; set; } = "Free";
        public decimal AmountPaid { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
