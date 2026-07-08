namespace AICareerCoach.BLL.DTOs.Admin
{
    public class ChatSessionAdminDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? Title { get; set; }
        public int MessageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PaginatedChatSessionsDto
    {
        public List<ChatSessionAdminDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class ChatMessageAdminDto
    {
        public int Id { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string? ToolName { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
