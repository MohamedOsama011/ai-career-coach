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
                    Price = 9.99m,
                    DurationMonths = 1,
                },
                new()
                {
                    Name = "Pro",
                    Price = 29.99m,
                    DurationMonths = 1,
                },
                new()
                {
                    Name = "Premium",
                    Price = 59.99m,
                    DurationMonths = 1,
                },
            };

            context.Subscriptions.AddRange(plans);
            await context.SaveChangesAsync();
        }
    }
}
