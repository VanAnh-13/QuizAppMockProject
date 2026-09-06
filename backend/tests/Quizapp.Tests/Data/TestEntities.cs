using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Data;

internal static class TestEntities
{
    public static Quiz Quiz() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Test quiz",
        Duration = 15
    };

    public static Question Question(QuestionType type = QuestionType.MultipleChoice) => new()
    {
        Id = Guid.NewGuid(),
        Content = "Test question",
        QuestionType = type
    };

    public static User User() => new()
    {
        Id = Guid.NewGuid(),
        Username = Guid.NewGuid().ToString("N"),
        Email = $"{Guid.NewGuid():N}@example.com",
        Password = "test-only-hash"
    };

    public static Role Role() => new()
    {
        Id = Guid.NewGuid(),
        RoleName = Guid.NewGuid().ToString("N")
    };

    public static Answer Answer(Question question) => new()
    {
        Id = Guid.NewGuid(),
        Text = "Test option",
        IsCorrect = true,
        QuestionId = question.Id,
        QuestionNavigation = question
    };

    public static QuizAttempt Attempt(Quiz quiz, User user) => new()
    {
        Id = Guid.NewGuid(),
        QuizId = quiz.Id,
        QuizNavigation = quiz,
        UserId = user.Id,
        UserNavigation = user,
        SubmitAt = DateTime.UtcNow,
        Score = 0
    };

    public static UserAnswer Selection(QuizAttempt attempt, Question question, Answer? answer = null,
        string? responseText = null) => new()
        {
            Id = Guid.NewGuid(),
            QuizAttemptId = attempt.Id,
            QuizAttemptNavigation = attempt,
            QuestionId = question.Id,
            QuestionNavigation = question,
            AnswerId = answer?.Id,
            ResponseText = responseText
        };
}
