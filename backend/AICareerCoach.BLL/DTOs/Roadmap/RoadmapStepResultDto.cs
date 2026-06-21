namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class RoadmapStepResultDto
    {
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public List<string> Resources { get; set; } = new();
        public string? Duration { get; set; }
    }
}
