using AICareerCoach.DAL;
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
            var activeSub = await _context.UserSubscriptions
                .Include(us => us.Subscription)
                .Where(us => us.UserId == userId && us.IsActive && us.EndDate > DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            if (activeSub?.Subscription != null)
            {
                var planLimits = PlanLimits.FromJson(activeSub.Subscription.LimitsJson);
                return feature switch
                {
                    Feature.InterviewSession => await CheckWithPlanLimitAsync(
                        userId, feature, planLimits.InterviewSessions,
                        () => _context.InterviewSessions.CountAsync(s => s.UserId == userId)),
                    Feature.RoadmapGeneration => await CheckWithPlanLimitAsync(
                        userId, feature, planLimits.RoadmapGenerations,
                        () => _context.UserRoadmaps.CountAsync(r => r.UserId == userId)),
                    Feature.RoadmapRescan => new GateResult(
                        Allowed: planLimits.RoadmapRescan,
                        Reason: planLimits.RoadmapRescan ? "ok" : "rescan_not_in_plan",
                        Used: 0,
                        Limit: planLimits.RoadmapRescan ? -1 : 0),
                    Feature.JobRecommendations => new GateResult(
                        Allowed: true,
                        Reason: "active_subscription",
                        Used: 0,
                        Limit: planLimits.JobRecommendations),
                    _ => new GateResult(Allowed: false, Reason: "unknown_feature", Used: 0, Limit: 0),
                };
            }

            return feature switch
            {
                Feature.InterviewSession => await CheckFreeLimitAsync(
                    userId, Feature.InterviewSession, FreeLimits.InterviewSessions,
                    () => _context.InterviewSessions.CountAsync(s => s.UserId == userId)),
                Feature.RoadmapGeneration => await CheckFreeLimitAsync(
                    userId, Feature.RoadmapGeneration, FreeLimits.RoadmapGenerations,
                    () => _context.UserRoadmaps.CountAsync(r => r.UserId == userId)),
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

        private async Task<GateResult> CheckWithPlanLimitAsync(
            string userId, Feature feature, int limit, Func<Task<int>> countFunc)
        {
            if (limit == -1)
            {
                _logger.LogInformation("Gate: User {UserId} has unlimited {Feature} via plan", userId, feature);
                return new GateResult(true, "unlimited_plan", 0, -1);
            }

            var used = await countFunc();
            if (used >= limit)
            {
                _logger.LogWarning("Gate blocked: User {UserId} {Feature} limit reached ({Used}/{Limit}) via plan", userId, feature, used, limit);
                return new GateResult(false, $"{feature.ToString().ToLower()}_limit_reached", used, limit);
            }
            return new GateResult(true, "ok", used, limit);
        }

        private async Task<GateResult> CheckFreeLimitAsync(
            string userId, Feature feature, int limit, Func<Task<int>> countFunc)
        {
            var used = await countFunc();
            if (used >= limit)
            {
                _logger.LogWarning("Gate blocked: User {UserId} {Feature} free limit reached ({Used}/{Limit})", userId, feature, used, limit);
                return new GateResult(false, $"{feature.ToString().ToLower()}_limit_reached", used, limit);
            }
            return new GateResult(true, "ok", used, limit);
        }
    }
}
