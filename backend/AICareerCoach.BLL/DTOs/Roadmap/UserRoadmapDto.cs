namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class UserRoadmapDto
    {
        public int Id { get; set; }
        public string TargetRole { get; set; } = string.Empty;
        public string TemplateTrack { get; set; } = string.Empty;
        public List<RoadmapStepResultDto> Steps { get; set; } = new();
        public List<SkillsCategoryDto> GapAnalysis { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public double? MatchScore { get; set; }
        public TemplateSnapshotDto? TemplateSnapshot { get; set; }
    }
}
