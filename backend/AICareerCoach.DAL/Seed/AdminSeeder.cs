using Microsoft.AspNetCore.Identity;
using AICareerCoach.DAL.Models;
using Microsoft.Extensions.Logging;

namespace AICareerCoach.DAL.Seed
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(UserManager<User> userManager, ILogger logger)
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count > 0)
            {
                logger.LogInformation("Admin user already exists ({Count}). Skipping admin seeding.", admins.Count);
                return;
            }

            var admin = new User
            {
                UserName = "admin@aicoach.com",
                Email = "admin@aicoach.com",
                FullName = "System Admin"
            };

            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
                logger.LogInformation("Default admin user created (admin@aicoach.com).");
            }
            else
            {
                logger.LogError("Failed to create default admin: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
