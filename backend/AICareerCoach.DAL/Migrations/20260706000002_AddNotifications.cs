using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Hand-written because EF tools are blocked by Windows Application
        /// Control policy on the .dll (per AGENTS.md). Creates the
        /// Notifications table for the Phase 8 broadcast/notification system.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE [Notifications] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [UserId] nvarchar(450) NOT NULL,
                    [Title] nvarchar(256) NOT NULL DEFAULT '',
                    [Body] nvarchar(max) NOT NULL DEFAULT '',
                    [Type] nvarchar(32) NOT NULL DEFAULT 'info',
                    [IsRead] bit NOT NULL DEFAULT 0,
                    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Notifications_AspNetUsers_UserId]
                        FOREIGN KEY ([UserId])
                        REFERENCES [AspNetUsers]([Id])
                        ON DELETE CASCADE
                );

                CREATE INDEX [IX_Notifications_UserId]
                    ON [Notifications] ([UserId]);

                CREATE INDEX [IX_Notifications_UserId_IsRead_CreatedAt]
                    ON [Notifications] ([UserId], [IsRead], [CreatedAt] DESC);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS [Notifications];
            ");
        }
    }
}
