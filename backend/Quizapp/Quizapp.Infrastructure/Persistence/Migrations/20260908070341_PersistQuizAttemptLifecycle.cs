using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizapp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistQuizAttemptLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmitAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Historical start times were not recorded. Preserve submitted results
            // and use their submission time as the legacy lifecycle boundary.
            migrationBuilder.Sql("""
                UPDATE [QuizAttempts]
                SET [StartedAt] = [SubmitAt], [ExpiresAt] = [SubmitAt]
                WHERE [SubmitAt] IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [QuizAttempts] WHERE [SubmitAt] IS NULL)
                    THROW 50001, 'Cannot roll back while unsubmitted quiz attempts exist.', 1;
                """);

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "QuizAttempts");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmitAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
