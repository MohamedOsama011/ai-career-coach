namespace AICareerCoach.BLL.DTOs.Admin
{
    public class AdminAuditLogDto
    {
        public int Id { get; set; }
        public string? AdminUserId { get; set; }
        public string AdminUserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PaginatedAuditLogsDto
    {
        public List<AdminAuditLogDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage => Page * PageSize < TotalCount;
    }
}
