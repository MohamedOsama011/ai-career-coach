namespace AICareerCoach.DAL.Entities
{
    public class UserRoadmap
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CvHash { get; set; } = string.Empty;
        public string TargetRole { get; set; } = string.Empty;
        public int? TemplateRoadmapId { get; set; }
        public string TemplateTrack { get; set; } = string.Empty;
        public string? TemplateSnapshotJson { get; set; }
        public string StepsJson { get; set; } = "[]";
        public string GapAnalysisJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
