using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeVision.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDateTimeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix DateTime columns from TEXT to timestamp for PostgreSQL compatibility
            // Use raw SQL with USING clause for explicit type conversion
            migrationBuilder.Sql(@"ALTER TABLE ""PullRequestAnalyses"" ALTER COLUMN ""ProcessedAt"" TYPE timestamp with time zone USING ""ProcessedAt""::timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""PullRequestAnalyses"" ALTER COLUMN ""CreatedAt"" TYPE timestamp with time zone USING ""CreatedAt""::timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""PullRequestAnalyses"" ALTER COLUMN ""UpdatedAt"" TYPE timestamp with time zone USING ""UpdatedAt""::timestamp with time zone;");
            migrationBuilder.Sql(@"ALTER TABLE ""AnalysisNotifications"" ALTER COLUMN ""CreatedAt"" TYPE timestamp with time zone USING ""CreatedAt""::timestamp with time zone;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert DateTime columns back to TEXT (for rollback)
            // Use raw SQL for explicit type conversion back to TEXT
            migrationBuilder.Sql(@"ALTER TABLE ""PullRequestAnalyses"" ALTER COLUMN ""ProcessedAt"" TYPE TEXT USING ""ProcessedAt""::TEXT;");
            migrationBuilder.Sql(@"ALTER TABLE ""PullRequestAnalyses"" ALTER COLUMN ""CreatedAt"" TYPE TEXT USING ""CreatedAt""::TEXT;");
            migrationBuilder.Sql(@"ALTER TABLE ""PullRequestAnalyses"" ALTER COLUMN ""UpdatedAt"" TYPE TEXT USING ""UpdatedAt""::TEXT;");
            migrationBuilder.Sql(@"ALTER TABLE ""AnalysisNotifications"" ALTER COLUMN ""CreatedAt"" TYPE TEXT USING ""CreatedAt""::TEXT;");
        }
    }
}
