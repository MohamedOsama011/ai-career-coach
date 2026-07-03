using AICareerCoach.DAL.Models;

namespace AICareerCoach.DAL.Entities
{
    public class ChatSession
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public List<ChatMessage> Messages { get; set; } = new();
    }
}
