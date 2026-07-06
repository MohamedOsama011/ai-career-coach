using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAuditLog : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Hand-written because EF tools are blocked by Windows Application
        /// Control policy on the .dll (per AGENTS.md). Creates the
        /// SubscriptionAuditLogs table for tracking all admin manual actions
        /// (activate, cancel, extend, mark-paid, refund) on subscriptions.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE [SubscriptionAuditLogs] (
                    [Id] int NOT NULL IDENTITY(1,1),
                    [AdminUserId] nvarchar(450) NULL,
                    [Action] nvarchar(64) NOT NULL DEFAULT '',
                    [UserSubscriptionId] int NULL,
                    [TargetUserId] nvarchar(450) NULL,
                    [PreviousValues] nvarchar(max) NULL,
                    [NewValues] nvarchar(max) NULL,
                    [Notes] nvarchar(max) NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT [PK_SubscriptionAuditLogs] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_SubscriptionAuditLogs_AspNetUsers_AdminUserId]
                        FOREIGN KEY ([AdminUserId])
                        REFERENCES [AspNetUsers]([Id])
                        ON DELETE NO ACTION,
                    CONSTRAINT [FK_SubscriptionAuditLogs_UserSubscriptions_UserSubscriptionId]
                        FOREIGN KEY ([UserSubscriptionId])
                        REFERENCES [UserSubscriptions]([Id])
                        ON DELETE NO ACTION
                );

                CREATE INDEX [IX_SubscriptionAuditLogs_UserSubscriptionId]
                    ON [SubscriptionAuditLogs] ([UserSubscriptionId]);

                CREATE INDEX [IX_SubscriptionAuditLogs_AdminUserId]
                    ON [SubscriptionAuditLogs] ([AdminUserId]);

                CREATE INDEX [IX_SubscriptionAuditLogs_CreatedAt]
                    ON [SubscriptionAuditLogs] ([CreatedAt] DESC);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS [SubscriptionAuditLogs];
            ");
        }
    }
}
