using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Quizapp.Tests.Data;

public class AttemptProgressMigrationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task Upgrading_initializes_legacy_drafts_and_rollback_refuses_to_lose_unfinished_attempts()
    {
        await using var db = database.CreateContext();
        var migrator = db.GetService<IMigrator>();
        const string previousMigration = "20260908070341_PersistQuizAttemptLifecycle";
        await migrator.MigrateAsync(previousMigration);
        var quiz = TestEntities.Quiz();
        var user = TestEntities.User();
        db.AddRange(quiz, user);
        await db.SaveChangesAsync();
        var attemptId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var expiresAt = startedAt.AddMinutes(10);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [QuizAttempts] ([Id], [QuizId], [UserId], [StartedAt], [ExpiresAt], [SubmitAt], [Score])
            VALUES ({attemptId}, {quiz.Id}, {user.Id}, {startedAt}, {expiresAt}, NULL, {0.0});
            """);

        await migrator.MigrateAsync();

        var attempt = await db.QuizAttempts.SingleAsync(a => a.Id == attemptId);
        Assert.Equal("[]", attempt.DraftAnswersJson);
        Assert.Equal(0, attempt.Revision);
        Assert.Null(attempt.PausedAt);
        Assert.Equal(startedAt, attempt.StartedAt);
        Assert.Equal(expiresAt, attempt.ExpiresAt);
        var rejected = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(previousMigration));
        Assert.Contains("Cannot roll back while unsubmitted quiz attempts exist", rejected.Message);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }
}
