namespace AICareerCoach.BLL.DTOs.Interview
{
    public class InterviewSessionDto
    {
        public int Id { get; set; }
        public string Track { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int QuestionsAsked { get; set; }
        public int MaxQuestions { get; set; } = 6;
        public List<InterviewMessageDto> Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class InterviewMessageDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public int TurnNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
