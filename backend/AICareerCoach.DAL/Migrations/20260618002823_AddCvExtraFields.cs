using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCvExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedData",
                table: "CVs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "CVs");

            migrationBuilder.DropColumn(
                name: "ExtractedData",
                table: "CVs");
        }
    }
}
