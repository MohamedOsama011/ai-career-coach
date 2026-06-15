using System.Text.Json;

namespace AICareerCoach.DAL.Entities
{
    public class RoadmapStep
    {
        public int Id { get; set; }

        public int RoadmapId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Level { get; set; } = string.Empty; 

        public string Resources { get; set; } = "[]"; 

        public int OrderIndex { get; set; }

        public Roadmap Roadmap { get; set; } = null!;
    }
}
