using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeVision.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBooleanColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix IsRead column from INTEGER to boolean for PostgreSQL compatibility
            // Use raw SQL with USING clause for explicit type conversion
            migrationBuilder.Sql(@"ALTER TABLE ""AnalysisNotifications"" ALTER COLUMN ""IsRead"" TYPE boolean USING ""IsRead""::boolean;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert IsRead column back to INTEGER (for rollback)
            // Use raw SQL for explicit type conversion back to INTEGER
            migrationBuilder.Sql(@"ALTER TABLE ""AnalysisNotifications"" ALTER COLUMN ""IsRead"" TYPE INTEGER USING CASE WHEN ""IsRead"" THEN 1 ELSE 0 END;");
        }
    }
}
