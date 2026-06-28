using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionAndFallbackFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FallbackCount",
                table: "InterviewSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "InterviewSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "UsedFallback",
                table: "InterviewSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FallbackCount",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "InterviewSessions");

            migrationBuilder.DropColumn(
                name: "UsedFallback",
                table: "InterviewSessions");
        }
    }
}
