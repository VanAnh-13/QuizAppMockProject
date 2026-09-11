using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quizapp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptPauseAndDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftAnswersJson",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSavedAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "QuizAttempts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuizSnapshotJson",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                table: "QuizAttempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [QuizAttempts] WHERE [SubmitAt] IS NULL)
                    THROW 50001, 'Cannot roll back while unsubmitted quiz attempts exist.', 1;
                """);

            migrationBuilder.DropColumn(
                name: "DraftAnswersJson",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "LastSavedAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "QuizSnapshotJson",
                table: "QuizAttempts");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "QuizAttempts");
        }
    }
}
