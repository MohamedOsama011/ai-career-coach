namespace AICareerCoach.BLL.DTOs.Admin
{
    public class HealthCheckDto
    {
        public HealthComponentStatus Db { get; set; } = new();
        public HealthComponentStatus Llm { get; set; } = new();
        public HealthComponentStatus JobProvider { get; set; } = new();
        public StorageHealthStatus Storage { get; set; } = new();
        public string Uptime { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime? LastSyncTime { get; set; }
        public bool LastSyncSuccess { get; set; }
    }

    public class HealthComponentStatus
    {
        public string Status { get; set; } = "healthy";
        public string? Message { get; set; }
        public long? LatencyMs { get; set; }
    }

    public class StorageHealthStatus
    {
        public string Status { get; set; } = "healthy";
        public string? Message { get; set; }
        public double UsedPercent { get; set; }
        public long UsedBytes { get; set; }
        public long TotalBytes { get; set; }
    }
}
