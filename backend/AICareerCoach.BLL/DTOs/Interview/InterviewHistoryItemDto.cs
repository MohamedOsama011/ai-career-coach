namespace AICareerCoach.BLL.DTOs.Interview
{
    public class InterviewHistoryItemDto
    {
        public int Id { get; set; }
        public string Track { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int QuestionsAsked { get; set; }
        public int MaxQuestions { get; set; } = 6;
        public int? OverallScore { get; set; }
        public string? LetterGrade { get; set; }
        public string? OverallSummary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
