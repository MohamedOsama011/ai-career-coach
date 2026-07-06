using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.BLL.DTOs.Chat
{
    public class SendChatMessageDto
    {
        [Required, MinLength(1), MaxLength(4000)]
        public string Message { get; set; } = string.Empty;
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty;
        public string? Content { get; set; }
        public List<string>? ToolsUsed { get; set; }
    }

    public class ChatSessionDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class ChatSessionSummaryDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
