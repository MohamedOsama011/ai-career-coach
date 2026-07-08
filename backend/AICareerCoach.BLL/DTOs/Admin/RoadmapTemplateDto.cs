namespace AICareerCoach.BLL.DTOs.Admin
{
    public class RoadmapTemplateDto
    {
        public int Id { get; set; }
        public string Track { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int StepsCount { get; set; }
        public bool HasEmbedding { get; set; }
        public DateTime? EmbeddingComputedAt { get; set; }
        public List<AdminRoadmapStepDto> Steps { get; set; } = new();
    }

    public class AdminRoadmapStepDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public List<string> Resources { get; set; } = new();
        public int OrderIndex { get; set; }
    }
}
