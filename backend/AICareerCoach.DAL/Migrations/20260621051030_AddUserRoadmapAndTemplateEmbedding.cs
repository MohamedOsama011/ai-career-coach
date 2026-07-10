using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoadmapAndTemplateEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadmapTemplateEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoadmapId = table.Column<int>(type: "int", nullable: false),
                    Embedding = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapTemplateEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoadmapTemplateEmbeddings_Roadmaps_RoadmapId",
                        column: x => x.RoadmapId,
                        principalTable: "Roadmaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoadmaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CvHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateRoadmapId = table.Column<int>(type: "int", nullable: true),
                    TemplateTrack = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemplateSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GapAnalysisJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoadmaps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapTemplateEmbeddings_RoadmapId",
                table: "RoadmapTemplateEmbeddings",
                column: "RoadmapId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadmapTemplateEmbeddings");

            migrationBuilder.DropTable(
                name: "UserRoadmaps");
        }
    }
}
