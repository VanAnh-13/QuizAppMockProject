using Microsoft.EntityFrameworkCore;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Data;

public class ProfileAndQuizFieldsPersistenceTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task Profile_and_quiz_fields_round_trip_and_user_activity_follows_status()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = Guid.NewGuid().ToString("N"),
            Email = $"{Guid.NewGuid():N}@example.com",
            Password = "test-only-hash",
            FullName = "Nguyễn Văn An",
            PhoneNumber = "+84901234567",
            DateOfBirth = new DateOnly(2000, 2, 29),
            Avatar = "/images/avatar.png",
            IsActive = true
        };
        var quiz = new Quiz { Id = Guid.NewGuid(), Title = "Quiz", Duration = 15, PassedScore = 120.5 };
        var question = new Question
        {
            Id = Guid.NewGuid(),
            Content = "Question",
            QuestionType = QuestionType.MultipleChoice,
            Level = QuestionLevel.Hard,
            Image = "/images/question.png"
        };

        await using var context = fixture.CreateContext();
        context.AddRange(user, quiz, question);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var savedUser = await context.Users.SingleAsync(value => value.Id == user.Id);
        Assert.Equal(user.FullName, savedUser.FullName);
        Assert.Equal(user.PhoneNumber, savedUser.PhoneNumber);
        Assert.Equal(user.DateOfBirth, savedUser.DateOfBirth);
        Assert.Equal(user.Avatar, savedUser.Avatar);
        Assert.True(savedUser.IsActive);
        Assert.Equal(UserStatus.Active, savedUser.Status);
        Assert.True(await context.Users.AnyAsync(value => value.Id == user.Id && value.IsActive));
        Assert.Equal(quiz.PassedScore, await context.Quizzes.Where(value => value.Id == quiz.Id).Select(value => value.PassedScore).SingleAsync());
        var savedQuestion = await context.Questions.SingleAsync(value => value.Id == question.Id);
        Assert.Equal(question.Level, savedQuestion.Level);
        Assert.Equal(question.Image, savedQuestion.Image);

        context.Entry(savedUser).Property(value => value.Status).CurrentValue = UserStatus.Deactivated;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.False((await context.Users.SingleAsync(value => value.Id == user.Id)).IsActive);
        Assert.False(await context.Users.AnyAsync(value => value.Id == user.Id && value.IsActive));
    }

    [SqlServerFact]
    public async Task Database_rejects_negative_passed_score_and_unknown_question_level()
    {
        await using (var context = fixture.CreateContext())
        {
            context.Quizzes.Add(new Quiz { Title = "Invalid threshold", Duration = 15, PassedScore = -1 });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        await using (var context = fixture.CreateContext())
        {
            context.Questions.Add(new Question
            {
                Content = "Invalid level",
                QuestionType = QuestionType.SingleChoice,
                Level = (QuestionLevel)4
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }
}
