using AICareerCoach.BLL.DTOs.Interview;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IAdminInterviewService
    {
        Task<PaginatedSessionsResult> GetSessionsAsync(
            int page = 1,
            int pageSize = 20,
            string? status = null,
            string? track = null,
            string? difficulty = null,
            DateTime? from = null,
            DateTime? to = null);

        Task<bool> DeleteSessionAsync(int sessionId);

        Task<bool> AbortSessionAsync(int sessionId);
    }
}
