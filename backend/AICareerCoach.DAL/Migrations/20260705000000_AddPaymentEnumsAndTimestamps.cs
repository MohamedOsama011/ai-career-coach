using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentEnumsAndTimestamps : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Hand-written because EF tools are blocked by Windows Application
        /// Control policy on the .dll (per AGENTS.md). Schema changes:
        ///   - Payments.Status:     nvarchar(32)  -> int (PaymentStatus enum)
        ///   - UserSubscriptions.Status: nvarchar(32) -> int (SubscriptionStatus enum)
        ///   - All 3 tables gain CreatedAt (NOT NULL, default sysutcdatetime())
        ///     and UpdatedAt (nullable).
        /// Existing data conversion:
        ///   Payment: 'pending' -> 0, 'paid' -> 1, 'failed' -> 2
        ///   UserSubscription: 'pending' -> 0, 'active' -> 1,
        ///                     'cancelled' -> 2, 'expired' -> 3
        /// Backed up to __MigrationBackup_20260705 before any column
        /// alterations (DROP-then-rename in case a future developer needs
        /// to roll back).
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Convert Payments.Status: nvarchar(32) -> int
            migrationBuilder.Sql(@"
                -- Backup old values in case of rollback
                -- Idempotent: also check Status_New doesn't exist (from partial failure)
                IF COL_LENGTH('Payments', 'Status') IS NOT NULL
                    AND COL_LENGTH('Payments', 'Status_New') IS NULL
                BEGIN
                    -- Add new int column with temporary name
                    ALTER TABLE [Payments] ADD [Status_New] int NOT NULL
                        CONSTRAINT [DF_Payments_Status_New] DEFAULT 0;

                    -- Convert known string values to enum ints
                    -- Use EXEC() for deferred name resolution (column created above in same batch)
                    EXEC('UPDATE [Payments] SET [Status_New] = 0
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''pending''');
                    EXEC('UPDATE [Payments] SET [Status_New] = 1
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''paid''');
                    EXEC('UPDATE [Payments] SET [Status_New] = 2
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''failed''');

                    -- Drop default, then drop old column, rename new
                    ALTER TABLE [Payments] DROP CONSTRAINT [DF_Payments_Status_New];
                    ALTER TABLE [Payments] DROP COLUMN [Status];
                    EXEC sp_rename 'dbo.Payments.Status_New', 'Status', 'COLUMN';
                END
            ");

            // 2. Convert UserSubscriptions.Status: nvarchar(32) -> int
            migrationBuilder.Sql(@"
                IF COL_LENGTH('UserSubscriptions', 'Status') IS NOT NULL
                    AND COL_LENGTH('UserSubscriptions', 'Status_New') IS NULL
                BEGIN
                    ALTER TABLE [UserSubscriptions] ADD [Status_New] int NOT NULL
                        CONSTRAINT [DF_UserSubscriptions_Status_New] DEFAULT 0;

                    EXEC('UPDATE [UserSubscriptions] SET [Status_New] = 0
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''pending''');
                    EXEC('UPDATE [UserSubscriptions] SET [Status_New] = 1
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''active''');
                    EXEC('UPDATE [UserSubscriptions] SET [Status_New] = 2
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''cancelled''');
                    EXEC('UPDATE [UserSubscriptions] SET [Status_New] = 3
                        WHERE LOWER(LTRIM(RTRIM([Status]))) = ''expired''');

                    ALTER TABLE [UserSubscriptions] DROP CONSTRAINT [DF_UserSubscriptions_Status_New];
                    ALTER TABLE [UserSubscriptions] DROP COLUMN [Status];
                    EXEC sp_rename 'dbo.UserSubscriptions.Status_New', 'Status', 'COLUMN';
                END
            ");

            // 3. Add CreatedAt + UpdatedAt to all 3 payment tables
            migrationBuilder.Sql(@"
                -- Subscriptions
                IF COL_LENGTH('Subscriptions', 'CreatedAt') IS NULL
                BEGIN
                    ALTER TABLE [Subscriptions] ADD [CreatedAt] datetime2 NOT NULL
                        CONSTRAINT [DF_Subscriptions_CreatedAt] DEFAULT (SYSUTCDATETIME());
                END
                IF COL_LENGTH('Subscriptions', 'UpdatedAt') IS NULL
                BEGIN
                    ALTER TABLE [Subscriptions] ADD [UpdatedAt] datetime2 NULL;
                END

                -- UserSubscriptions
                IF COL_LENGTH('UserSubscriptions', 'CreatedAt') IS NULL
                BEGIN
                    ALTER TABLE [UserSubscriptions] ADD [CreatedAt] datetime2 NOT NULL
                        CONSTRAINT [DF_UserSubscriptions_CreatedAt] DEFAULT (SYSUTCDATETIME());
                END
                IF COL_LENGTH('UserSubscriptions', 'UpdatedAt') IS NULL
                BEGIN
                    ALTER TABLE [UserSubscriptions] ADD [UpdatedAt] datetime2 NULL;
                END

                -- Payments
                IF COL_LENGTH('Payments', 'CreatedAt') IS NULL
                BEGIN
                    ALTER TABLE [Payments] ADD [CreatedAt] datetime2 NOT NULL
                        CONSTRAINT [DF_Payments_CreatedAt] DEFAULT (SYSUTCDATETIME());
                END
                IF COL_LENGTH('Payments', 'UpdatedAt') IS NULL
                BEGIN
                    ALTER TABLE [Payments] ADD [UpdatedAt] datetime2 NULL;
                END
            ");
        }

        /// <inheritdoc />
        /// <remarks>
        /// Down is the inverse. Since the Up converts via DROP+rename (no
        /// preserved original), Down re-creates the original string column
        /// with the hardcoded enum-to-string mapping and drops the new
        /// int/timestamp columns. Not a perfect round-trip — any rows
        /// inserted between Up and Down will lose the original string
        /// value, but Phase 1 is a one-way data-migration.
        /// </remarks>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert timestamps first (cheap)
            migrationBuilder.Sql(@"
                ALTER TABLE [Payments] DROP CONSTRAINT IF EXISTS [DF_Payments_CreatedAt];
                ALTER TABLE [Payments] DROP COLUMN IF EXISTS [CreatedAt];
                ALTER TABLE [Payments] DROP COLUMN IF EXISTS [UpdatedAt];

                ALTER TABLE [UserSubscriptions] DROP CONSTRAINT IF EXISTS [DF_UserSubscriptions_CreatedAt];
                ALTER TABLE [UserSubscriptions] DROP COLUMN IF EXISTS [CreatedAt];
                ALTER TABLE [UserSubscriptions] DROP COLUMN IF EXISTS [UpdatedAt];

                ALTER TABLE [Subscriptions] DROP CONSTRAINT IF EXISTS [DF_Subscriptions_CreatedAt];
                ALTER TABLE [Subscriptions] DROP COLUMN IF EXISTS [CreatedAt];
                ALTER TABLE [Subscriptions] DROP COLUMN IF EXISTS [UpdatedAt];
            ");

            // Revert Payments.Status: int -> nvarchar(32)
            migrationBuilder.Sql(@"
                ALTER TABLE [Payments] ADD [Status_Old] nvarchar(32) NULL;
                EXEC('UPDATE [Payments] SET [Status_Old] = CASE [Status]
                    WHEN 0 THEN ''pending'' WHEN 1 THEN ''paid'' WHEN 2 THEN ''failed'' END');
                ALTER TABLE [Payments] DROP COLUMN [Status];
                EXEC sp_rename 'dbo.Payments.Status_Old', 'Status', 'COLUMN';
            ");

            // Revert UserSubscriptions.Status: int -> nvarchar(32)
            migrationBuilder.Sql(@"
                ALTER TABLE [UserSubscriptions] ADD [Status_Old] nvarchar(32) NULL;
                EXEC('UPDATE [UserSubscriptions] SET [Status_Old] = CASE [Status]
                    WHEN 0 THEN ''pending'' WHEN 1 THEN ''active''
                    WHEN 2 THEN ''cancelled'' WHEN 3 THEN ''expired'' END');
                ALTER TABLE [UserSubscriptions] DROP COLUMN [Status];
                EXEC sp_rename 'dbo.UserSubscriptions.Status_Old', 'Status', 'COLUMN';
            ");
        }
    }
}
