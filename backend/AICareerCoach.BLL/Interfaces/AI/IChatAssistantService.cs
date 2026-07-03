using AICareerCoach.BLL.DTOs.Chat;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IChatAssistantService
    {
        Task<ChatSessionDto> CreateSessionAsync(string userId);
        Task<ChatSessionDto> SendMessageAsync(string userId, int sessionId, string message);
        Task<ChatSessionDto> GetSessionAsync(string userId, int sessionId);
        Task<List<ChatSessionSummaryDto>> GetUserSessionsAsync(string userId);
    }
}
