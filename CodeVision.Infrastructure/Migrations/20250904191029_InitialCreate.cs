using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeVision.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PullRequestAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepoName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PrNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    PrTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PrAuthor = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    QualityScore = table.Column<int>(type: "INTEGER", nullable: false),
                    RoslynFindings = table.Column<string>(type: "jsonb", nullable: false),
                    GptSuggestions = table.Column<string>(type: "jsonb", nullable: false),
                    RiskLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawDiff = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullRequestAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalysisNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PullRequestAnalysisId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysisNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysisNotifications_PullRequestAnalyses_PullRequestAnalysisId",
                        column: x => x.PullRequestAnalysisId,
                        principalTable: "PullRequestAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNotifications_CreatedAt",
                table: "AnalysisNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNotifications_IsRead",
                table: "AnalysisNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysisNotifications_PullRequestAnalysisId",
                table: "AnalysisNotifications",
                column: "PullRequestAnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestAnalyses_ProcessedAt",
                table: "PullRequestAnalyses",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestAnalyses_QualityScore",
                table: "PullRequestAnalyses",
                column: "QualityScore");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestAnalyses_RepoName_PrNumber",
                table: "PullRequestAnalyses",
                columns: new[] { "RepoName", "PrNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestAnalyses_RiskLevel",
                table: "PullRequestAnalyses",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_PullRequestAnalyses_Status",
                table: "PullRequestAnalyses",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysisNotifications");

            migrationBuilder.DropTable(
                name: "PullRequestAnalyses");
        }
    }
}
