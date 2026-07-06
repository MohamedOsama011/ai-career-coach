using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using AICareerCoach.BLL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.BLL.Services
{
    public class SubscriptionGateService : ISubscriptionGateService
    {
        private readonly AICareerCoachDbContext _context;
        private readonly ILogger<SubscriptionGateService> _logger;

        public SubscriptionGateService(
            AICareerCoachDbContext context,
            ILogger<SubscriptionGateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasActiveSubscriptionAsync(string userId)
        {
            return await _context.UserSubscriptions
                .AnyAsync(us => us.UserId == userId
                             && us.IsActive
                             && us.EndDate > DateTime.UtcNow);
        }

        public async Task<GateResult> CheckAccessAsync(string userId, Feature feature)
        {
            if (await HasActiveSubscriptionAsync(userId))
            {
                _logger.LogInformation("Gate: User {UserId} has active sub, allowed {Feature}", userId, feature);
                return new GateResult(Allowed: true, Reason: "active_subscription", Used: 0, Limit: -1);
            }

            return feature switch
            {
                Feature.InterviewSession => await CheckInterviewAsync(userId),
                Feature.RoadmapGeneration => await CheckRoadmapGenerationAsync(userId),
                Feature.RoadmapRescan => new GateResult(
                    Allowed: false,
                    Reason: "rescan_is_paid",
                    Used: 0,
                    Limit: 0),
                Feature.JobRecommendations => new GateResult(
                    Allowed: true,
                    Reason: "limited_view",
                    Used: 0,
                    Limit: FreeLimits.JobRecommendations),
                _ => new GateResult(Allowed: false, Reason: "unknown_feature", Used: 0, Limit: 0),
            };
        }

        private async Task<GateResult> CheckInterviewAsync(string userId)
        {
            var used = await _context.InterviewSessions.CountAsync(s => s.UserId == userId);
            var limit = FreeLimits.InterviewSessions;
            if (used >= limit)
            {
                _logger.LogWarning("Gate blocked: User {UserId} interview limit reached ({Used}/{Limit})", userId, used, limit);
                return new GateResult(false, "interview_limit_reached", used, limit);
            }
            return new GateResult(true, "ok", used, limit);
        }

        private async Task<GateResult> CheckRoadmapGenerationAsync(string userId)
        {
            var used = await _context.UserRoadmaps.CountAsync(r => r.UserId == userId);
            var limit = FreeLimits.RoadmapGenerations;
            if (used >= limit)
            {
                _logger.LogWarning("Gate blocked: User {UserId} roadmap limit reached ({Used}/{Limit})", userId, used, limit);
                return new GateResult(false, "roadmap_limit_reached", used, limit);
            }
            return new GateResult(true, "ok", used, limit);
        }
    }
}
