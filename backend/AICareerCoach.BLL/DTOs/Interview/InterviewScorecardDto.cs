namespace AICareerCoach.BLL.DTOs.Interview
{
    public class InterviewScorecardDto
    {
        public int OverallScore { get; set; }
        public string LetterGrade { get; set; } = string.Empty;
        public string OverallSummary { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> AreasForImprovement { get; set; } = new();
        public List<QuestionAnalysisItemDto> QuestionAnalysis { get; set; } = new();
    }
}
