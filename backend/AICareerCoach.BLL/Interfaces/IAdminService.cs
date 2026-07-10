using AICareerCoach.BLL.DTOs.Admin;

namespace AICareerCoach.BLL.Interfaces
{
    public interface IAdminService
    {
        Task<DashboardStatisticsDto> GetDashboardStatisticsAsync();
        Task<List<AdminUserDto>> GetAllUsersAsync();
        Task<bool> DeleteUserAsync(string id);
        Task<bool> ChangeUserRoleAsync(string id, string role);
        Task<List<CVAdminDto>> GetAllCVsAsync();
        Task<bool> DeleteCVAsync(int id);
        Task<DownloadCVDto?> DownloadCVAsync(int id);
        Task<List<UserManagementDto>> GetUserManagementAsync();
        Task<List<SyncLogDto>> GetSyncLogsAsync(int count = 50);
        Task<UserDetailDto?> GetUserDetailAsync(string id);
        Task ClearCacheAsync(int? userId);
        Task LogAuditAsync(string adminUserId, string action, string targetType, string? targetId, string? details);
        Task<PaginatedAuditLogsDto> GetAuditLogsAsync(int page, int pageSize, string? action, string? adminId);
        Task<HealthCheckDto> GetHealthAsync();
        Task<ReportsDto> GetReportsAsync();
        Task<byte[]> ExportCsvAsync(string reportType);
        Task SendBroadcastToAllAsync(string title, string body, string type);
        Task SendBroadcastToPlanAsync(string planName, string title, string body, string type);
        Task SendBroadcastToUserAsync(string userId, string title, string body, string type);
        Task<PaginatedChatSessionsDto> GetChatSessionsAsync(int page, int pageSize);
        Task<List<ChatMessageAdminDto>> GetChatMessagesAsync(int sessionId);
    }
}
