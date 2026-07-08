namespace AICareerCoach.BLL.DTOs.Notification
{
    public class BroadcastRequestDto
    {
        public string TargetType { get; set; } = "all";
        public string? TargetValue { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = "broadcast";
    }
}
