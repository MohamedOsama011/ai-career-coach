namespace AICareerCoach.BLL.DTOs.User
{
    public class UserProfileDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CareerGoal { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CvCount { get; set; }
        public IList<string>? Roles { get; set; }
        public bool HasCV { get; set; }
    }
}
