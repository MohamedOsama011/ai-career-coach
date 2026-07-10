namespace AICareerCoach.BLL.DTOs.Interview
{
    public class InterviewSessionAdminDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Track { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int QuestionsAsked { get; set; }
        public int MaxQuestions { get; set; } = 6;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Duration { get; set; } = string.Empty;
        public int MessageCount { get; set; }
        public bool HasScorecard { get; set; }
    }

    public class PaginatedSessionsResult
    {
        public List<InterviewSessionAdminDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
