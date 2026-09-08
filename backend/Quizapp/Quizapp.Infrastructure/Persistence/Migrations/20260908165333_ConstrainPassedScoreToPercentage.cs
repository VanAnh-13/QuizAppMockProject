using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizapp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConstrainPassedScoreToPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Quizzes_PassedScore",
                table: "Quizzes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quizzes_PassedScore",
                table: "Quizzes",
                sql: "[PassedScore] IS NULL OR ([PassedScore] >= 0 AND [PassedScore] <= 100)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Quizzes_PassedScore",
                table: "Quizzes");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Quizzes_PassedScore",
                table: "Quizzes",
                sql: "[PassedScore] IS NULL OR [PassedScore] >= 0");
        }
    }
}
