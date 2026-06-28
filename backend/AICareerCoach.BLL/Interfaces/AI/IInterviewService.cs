using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.DTOs.Roadmap;

namespace AICareerCoach.BLL.Interfaces.AI
{
    public interface IInterviewService
    {
        Task<InterviewOptionsDto> GetOptionsAsync();
        Task<InterviewSessionDto> StartSessionAsync(string userId, StartSessionRequestDto request);
        Task<InterviewSessionDto?> GetActiveSessionAsync(string userId);
        Task<InterviewSessionDto> SubmitAnswerAsync(string userId, int sessionId, SubmitAnswerRequestDto request);
        Task<InterviewScorecardDto> GetScorecardAsync(string userId, int sessionId);
        Task<List<InterviewHistoryItemDto>> GetHistoryAsync(string userId);
        Task<UserRoadmapDto> ConvertScorecardToRoadmapAsync(string userId, int sessionId);
    }
}
