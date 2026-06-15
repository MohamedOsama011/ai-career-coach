namespace AICareerCoach.DAL.Entities
{
    public class Roadmap
    {
        public int Id { get; set; }

        public string Track { get; set; } = string.Empty; 

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public List<RoadmapStep> Steps { get; set; } = new();
    }
}
