using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddJobExternalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractType",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Jobs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "Jobs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRemote",
                table: "Jobs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Jobs",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fetched = table.Column<int>(type: "int", nullable: false),
                    New = table.Column<int>(type: "int", nullable: false),
                    Skipped = table.Column<int>(type: "int", nullable: false),
                    Embedded = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<int>(type: "int", nullable: false),
                    ErrorMessages = table.Column<string>(type: "nvarchar(max)", maxLength: -1, nullable: true),
                    Duration = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_ExternalId_Source",
                table: "Jobs",
                columns: new[] { "ExternalId", "Source" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobSyncLogs_SyncedAt",
                table: "JobSyncLogs",
                column: "SyncedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSyncLogs");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_ExternalId_Source",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ContractType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "IsRemote",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Jobs");
        }
    }
}
