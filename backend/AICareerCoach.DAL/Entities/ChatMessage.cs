namespace AICareerCoach.DAL.Entities
{
    public enum ChatMessageRole
    {
        User = 0,
        Assistant = 1,
        Tool = 2
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public ChatMessageRole Role { get; set; }
        public string? Content { get; set; }
        public string? ToolCallsJson { get; set; }
        public string? ToolCallId { get; set; }
        public string? ToolName { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ChatSession Session { get; set; } = null!;
    }
}
