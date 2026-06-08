using System.Text.Json;

namespace AICareerCoach.BLL.DTOs.Roadmap
{
    public class RoadmapStepDto
    {
        public int Id { get; set; }
        public int RoadmapId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public List<string> Resources { get; set; } = new();
        public int OrderIndex { get; set; }
    }
}
