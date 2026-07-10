using AICareerCoach.BLL.DTOs.Interview;
using AICareerCoach.BLL.Interfaces;
using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class AdminInterviewService : IAdminInterviewService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly ILogger<AdminInterviewService> _logger;

        public AdminInterviewService(AICareerCoachDbContext context, ILogger<AdminInterviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaginatedSessionsResult> GetSessionsAsync(
            int page = 1,
            int pageSize = 20,
            string? status = null,
            string? track = null,
            string? difficulty = null,
            DateTime? from = null,
            DateTime? to = null)
        {
            var query = _context.InterviewSessions
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Scorecard)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InterviewStatus>(status, out var statusEnum))
                query = query.Where(s => s.Status == statusEnum);

            if (!string.IsNullOrWhiteSpace(track) && Enum.TryParse<InterviewTrack>(track, out var trackEnum))
                query = query.Where(s => s.Track == trackEnum);

            if (!string.IsNullOrWhiteSpace(difficulty) && Enum.TryParse<InterviewDifficulty>(difficulty, out var diffEnum))
                query = query.Where(s => s.Difficulty == diffEnum);

            if (from.HasValue)
                query = query.Where(s => s.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(s => s.CreatedAt <= to.Value);

            var totalCount = await query.CountAsync();

            var rawData = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.UserId,
                    s.User!.FullName,
                    Email = s.User.Email ?? "",
                    s.Track,
                    s.Difficulty,
                    s.TargetRole,
                    s.Status,
                    s.QuestionsAsked,
                    s.MaxQuestions,
                    s.CreatedAt,
                    s.CompletedAt,
                    MessageCount = s.Messages.Count,
                    HasScorecard = s.Scorecard != null,
                })
                .ToListAsync();

            var items = rawData.Select(s => new InterviewSessionAdminDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserName = s.FullName,
                UserEmail = s.Email,
                Track = s.Track.ToString(),
                Difficulty = s.Difficulty.ToString(),
                TargetRole = s.TargetRole,
                Status = s.Status.ToString(),
                QuestionsAsked = s.QuestionsAsked,
                MaxQuestions = s.MaxQuestions,
                CreatedAt = s.CreatedAt,
                CompletedAt = s.CompletedAt,
                Duration = s.Status == InterviewStatus.Active
                    ? FormatDuration(DateTime.UtcNow - s.CreatedAt)
                    : FormatDuration((s.CompletedAt ?? s.CreatedAt) - s.CreatedAt),
                MessageCount = s.MessageCount,
                HasScorecard = s.HasScorecard,
            }).ToList();

            return new PaginatedSessionsResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<bool> DeleteSessionAsync(int sessionId)
        {
            var session = await _context.InterviewSessions.FindAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Admin attempted to delete non-existent session {SessionId}", sessionId);
                return false;
            }

            _context.InterviewSessions.Remove(session);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Admin deleted session {SessionId} for user {UserId}", sessionId, session.UserId);
            return true;
        }

        public async Task<bool> AbortSessionAsync(int sessionId)
        {
            var session = await _context.InterviewSessions.FindAsync(sessionId);
            if (session == null)
            {
                _logger.LogWarning("Admin attempted to abort non-existent session {SessionId}", sessionId);
                return false;
            }

            if (session.Status != InterviewStatus.Active)
            {
                _logger.LogWarning("Admin attempted to abort non-active session {SessionId} (status: {Status})",
                    sessionId, session.Status);
                return false;
            }

            session.Status = InterviewStatus.Abandoned;
            await _context.SaveChangesAsync();

            _logger.LogWarning("Admin aborted session {SessionId} for user {UserId}", sessionId, session.UserId);
            return true;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1)
                return "<1 min";
            if (duration.TotalHours < 1)
                return $"{(int)duration.TotalMinutes} min";
            return $"{(int)duration.TotalHours}h {(int)duration.Minutes % 60}m";
        }
    }
}
