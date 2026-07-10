using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.DAL.Seed
{
    public static class SubscriptionSeeder
    {
        public static async Task SeedAsync(AICareerCoachDbContext context)
        {
            if (await context.Subscriptions.AnyAsync()) return;

            var plans = new List<Subscription>
            {
                new()
                {
                    Name = "Basic",
                    Price = 0m,
                    DurationMonths = 1,
                    LimitsJson = new PlanLimits
                    {
                        InterviewSessions = 1,
                        RoadmapGenerations = 1,
                        JobRecommendations = 3,
                        RoadmapRescan = false,
                    }.ToJson(),
                },
                new()
                {
                    Name = "Pro",
                    Price = 399m,
                    DurationMonths = 1,
                    LimitsJson = new PlanLimits
                    {
                        InterviewSessions = 10,
                        RoadmapGenerations = 5,
                        JobRecommendations = 10,
                        RoadmapRescan = true,
                    }.ToJson(),
                },
                new()
                {
                    Name = "Premium",
                    Price = 999m,
                    DurationMonths = 1,
                    LimitsJson = new PlanLimits
                    {
                        InterviewSessions = -1,
                        RoadmapGenerations = -1,
                        JobRecommendations = -1,
                        RoadmapRescan = true,
                    }.ToJson(),
                },
            };

            context.Subscriptions.AddRange(plans);
            await context.SaveChangesAsync();
        }
    }
}
