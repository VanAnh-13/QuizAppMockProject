using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Data;

public class ProfileSchemaMigrationTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task New_migration_preserves_existing_users_quizzes_and_questions()
    {
        await using var context = fixture.CreateContext();
        var migrator = context.GetService<IMigrator>();
        // This fixture owns a new isolated database; no application database is used.
        await migrator.MigrateAsync("20260905153421_AddUserEmail");

        var user = TestEntities.User();
        var quiz = TestEntities.Quiz();
        var question = TestEntities.Question();
        var now = DateTime.UtcNow;
        const int activeStatus = (int)UserStatus.Active;

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Users] ([Id], [Username], [Email], [Password], [Status], [CreateAt], [UpdateAt])
            VALUES ({user.Id}, {user.Username}, {user.Email}, {user.Password}, {activeStatus}, {now}, {now});
            INSERT INTO [Quizzes] ([Id], [Title], [Duration], [IsActive], [CreateAt], [UpdateAt])
            VALUES ({quiz.Id}, {quiz.Title}, {quiz.Duration}, {false}, {now}, {now});
            INSERT INTO [Questions] ([Id], [Content], [QuestionType], [IsActive])
            VALUES ({question.Id}, {question.Content}, {(int)question.QuestionType}, {false});
            """);

        await migrator.MigrateAsync();

        var savedUser = await context.Users.SingleAsync(value => value.Id == user.Id);
        Assert.Equal(user.Username, savedUser.Username);
        Assert.Equal(user.Email, savedUser.Email);
        Assert.True(savedUser.IsActive);
        Assert.Null(savedUser.FullName);
        Assert.Null(savedUser.PhoneNumber);
        Assert.Null(savedUser.DateOfBirth);
        Assert.Null(savedUser.Avatar);
        var savedQuiz = await context.Quizzes.SingleAsync(value => value.Id == quiz.Id);
        Assert.Equal(quiz.Title, savedQuiz.Title);
        Assert.Null(savedQuiz.PassedScore);
        var savedQuestion = await context.Questions.SingleAsync(value => value.Id == question.Id);
        Assert.Equal(question.Content, savedQuestion.Content);
        Assert.Null(savedQuestion.Image);
        Assert.Null(savedQuestion.Level);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}
