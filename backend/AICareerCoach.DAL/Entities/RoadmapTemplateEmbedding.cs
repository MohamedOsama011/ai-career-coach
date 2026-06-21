namespace AICareerCoach.DAL.Entities
{
    public class RoadmapTemplateEmbedding
    {
        public int Id { get; set; }
        public int RoadmapId { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public DateTime ComputedAt { get; set; } = DateTime.UtcNow;
        public Roadmap Roadmap { get; set; } = null!;
    }
}
