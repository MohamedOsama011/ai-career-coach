namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class TemplateSnapshotDto
    {
        public int Id { get; set; }
        public string Track { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<RoadmapStepDto> Steps { get; set; } = new();
    }
}
