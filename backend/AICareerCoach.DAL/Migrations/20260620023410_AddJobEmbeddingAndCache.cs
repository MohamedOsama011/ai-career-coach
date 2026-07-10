using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AICareerCoach.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddJobEmbeddingAndCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobId = table.Column<int>(type: "int", nullable: false),
                    Embedding = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobEmbeddings_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobRecommendationCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CvHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRecommendationCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobRecommendationCaches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobEmbeddings_JobId",
                table: "JobEmbeddings",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobRecommendationCaches_UserId",
                table: "JobRecommendationCaches",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobEmbeddings");

            migrationBuilder.DropTable(
                name: "JobRecommendationCaches");
        }
    }
}
