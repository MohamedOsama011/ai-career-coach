

namespace AICareerCoach.BLL.DTOs.Job
{
    public class JobRecommendationDto
    {
        public int JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CompanyLogoUrl { get; set; }
        public decimal Salary { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? ExternalUrl { get; set; }

        public int MatchScore { get; set; }
        public string MatchExplanation { get; set; } = string.Empty;
        public List<string> MissingSkills { get; set; } = new();
    }
}
