namespace AICareerCoach.BLL.DTOs.Interview
{
    public class InterviewOptionsDto
    {
        public List<InterviewOptionItem> Tracks { get; set; } = new();
        public List<InterviewOptionItem> Difficulties { get; set; } = new();
    }

    public class InterviewOptionItem
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
