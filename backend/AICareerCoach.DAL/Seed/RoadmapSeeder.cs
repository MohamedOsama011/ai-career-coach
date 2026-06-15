using AICareerCoach.DAL.Data;
using AICareerCoach.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AICareerCoach.DAL.Seed
{
    public static class RoadmapSeeder
    {
        public static async Task SeedAsync(AICareerCoachDbContext context)
        {
            if (await context.Roadmaps.AnyAsync()) return;

            var backendRoadmap = new Roadmap
            {
                Track = "Backend",
                Title = "Backend .NET Developer",
                Description = "Complete roadmap to become a professional .NET Backend Developer",
                OrderIndex = 1,
                Steps = new List<RoadmapStep>
            {
                new() {
                    Title = "C# Fundamentals",
                    Description = "Master C# syntax, OOP, LINQ, and async/await",
                    Level = "Beginner",
                    OrderIndex = 1,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/dotnet/csharp", "https://www.pluralsight.com" })
                },
                new() {
                    Title = "ASP.NET Core Web API",
                    Description = "Build RESTful APIs, middleware, routing, and filters",
                    Level = "Intermediate",
                    OrderIndex = 2,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/aspnet/core" })
                },
                new() {
                    Title = "Entity Framework Core",
                    Description = "ORM, migrations, relationships, performance tuning",
                    Level = "Intermediate",
                    OrderIndex = 3,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://learn.microsoft.com/ef/core" })
                },
                new() {
                    Title = "SQL Server & T-SQL",
                    Description = "Queries, stored procedures, indexes, optimization",
                    Level = "Intermediate",
                    OrderIndex = 4,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://www.sqlservertutorial.net" })
                },
                new() {
                    Title = "Design Patterns & SOLID",
                    Description = "Repository, Unit of Work, DI, Clean Architecture",
                    Level = "Intermediate",
                    OrderIndex = 5,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://refactoring.guru/design-patterns" })
                },
                new() {
                    Title = "Auth & Security",
                    Description = "JWT, OAuth2, Identity, HTTPS, CORS",
                    Level = "Advanced",
                    OrderIndex = 6,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://jwt.io" })
                },
                new() {
                    Title = "Docker & Deployment",
                    Description = "Containerize APIs, deploy to Azure/Railway",
                    Level = "Advanced",
                    OrderIndex = 7,
                    Resources = JsonSerializer.Serialize(new List<string> { "https://docs.docker.com" })
                }
            }
            };

            context.Roadmaps.Add(backendRoadmap);
            await context.SaveChangesAsync();
        }
    }
}
