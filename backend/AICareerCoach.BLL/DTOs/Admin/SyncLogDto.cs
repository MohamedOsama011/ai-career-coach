namespace AICareerCoach.BLL.DTOs.Admin
{
    public class SyncLogDto
    {
        public int Id { get; set; }
        public DateTime SyncedAt { get; set; }
        public string Status { get; set; } = "Success";
        public int FetchedCount { get; set; }
        public int NewCount { get; set; }
        public int SkippedCount { get; set; }
        public int EmbeddedCount { get; set; }
        public int ErrorCount { get; set; }
        public string? ErrorMessages { get; set; }
        public long DurationMs { get; set; }
    }
}
