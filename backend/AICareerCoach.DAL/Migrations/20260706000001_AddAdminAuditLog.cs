using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLog : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Hand-written because EF tools are blocked by Windows Application
        /// Control policy on the .dll (per AGENTS.md). Creates the
        /// AdminAuditLogs table for tracking all admin actions
        /// (delete user, change role, delete CV, clear cache, etc.).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE [AdminAuditLogs] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [AdminUserId] nvarchar(450) NULL,
                    [Action] nvarchar(64) NOT NULL DEFAULT '',
                    [TargetType] nvarchar(64) NOT NULL DEFAULT '',
                    [TargetId] nvarchar(450) NULL,
                    [Details] nvarchar(max) NULL,
                    [Timestamp] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT [PK_AdminAuditLogs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_AdminAuditLogs_AspNetUsers_AdminUserId]
                        FOREIGN KEY ([AdminUserId])
                        REFERENCES [AspNetUsers]([Id])
                        ON DELETE NO ACTION
                );

                CREATE INDEX [IX_AdminAuditLogs_AdminUserId]
                    ON [AdminAuditLogs] ([AdminUserId]);

                CREATE INDEX [IX_AdminAuditLogs_TargetType]
                    ON [AdminAuditLogs] ([TargetType]);

                CREATE INDEX [IX_AdminAuditLogs_Timestamp]
                    ON [AdminAuditLogs] ([Timestamp] DESC);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS [AdminAuditLogs];
            ");
        }
    }
}
