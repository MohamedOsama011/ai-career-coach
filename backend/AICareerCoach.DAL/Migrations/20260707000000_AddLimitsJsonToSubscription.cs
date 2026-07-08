using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddLimitsJsonToSubscription : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Hand-written because EF tools are blocked by Windows Application
        /// Control policy (per AGENTS.md). Adds LimitsJson column for
        /// per-plan feature limits, and updates existing price data to
        /// match the 3-tier subscription model: Basic (Free), Pro (EGP 399),
        /// Premium (EGP 999).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. Add LimitsJson column
                ALTER TABLE [Subscriptions]
                ADD [LimitsJson] nvarchar(max) NULL;

                -- 2. Update existing plans with per-plan limits JSON
                -- Basic: 1 interview, 1 roadmap, 3 jobs, no rescan
                UPDATE [Subscriptions]
                SET [Price] = 0,
                    [LimitsJson] = N'{""InterviewSessions"":1,""RoadmapGenerations"":1,""JobRecommendations"":3,""RoadmapRescan"":false}'
                WHERE [Name] = N'Basic';

                -- Pro: 10 interviews, 5 roadmaps, 10 jobs, rescan on
                UPDATE [Subscriptions]
                SET [Price] = 399,
                    [LimitsJson] = N'{""InterviewSessions"":10,""RoadmapGenerations"":5,""JobRecommendations"":10,""RoadmapRescan"":true}'
                WHERE [Name] = N'Pro';

                -- Premium: unlimited, rescan on
                UPDATE [Subscriptions]
                SET [Price] = 999,
                    [LimitsJson] = N'{""InterviewSessions"":-1,""RoadmapGenerations"":-1,""JobRecommendations"":-1,""RoadmapRescan"":true}'
                WHERE [Name] = N'Premium';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Restore original prices (remove limits column is irreversible if data loss)
                UPDATE [Subscriptions]
                SET [Price] = CASE [Name]
                    WHEN N'Basic' THEN 9.99
                    WHEN N'Pro' THEN 29.99
                    WHEN N'Premium' THEN 59.99
                    ELSE [Price]
                END;

                ALTER TABLE [Subscriptions]
                DROP COLUMN IF EXISTS [LimitsJson];
            ");
        }
    }
}
