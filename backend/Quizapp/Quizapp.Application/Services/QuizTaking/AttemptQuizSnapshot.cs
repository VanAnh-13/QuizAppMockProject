using System.Text.Json;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Services.QuizTaking;

// Stored only on the server: the snapshot includes grading keys and must never be returned as a response.
internal sealed record AttemptQuizSnapshot(Guid Id, string Title, string? Description, int Duration,
    string? Image, double? PassedScore, AttemptQuestionSnapshot[] Questions)
{
    public static string Capture(Quiz quiz) => JsonSerializer.Serialize(new AttemptQuizSnapshot(
        quiz.Id, quiz.Title, quiz.Description, quiz.Duration, quiz.Image, quiz.PassedScore,
        quiz.QuizQuestions.Where(qq => qq.QuestionNavigation.IsActive).OrderBy(qq => qq.Order)
            .Select(qq => new AttemptQuestionSnapshot(
                qq.QuestionId, qq.QuestionNavigation.Content, qq.QuestionNavigation.Image,
                qq.QuestionNavigation.Level, qq.QuestionNavigation.QuestionType, qq.Order,
                qq.QuestionNavigation.Answers.Where(a => a.IsActive)
                    .Select(a => new AttemptAnswerSnapshot(a.Id, a.Text, a.IsCorrect)).ToArray()))
            .ToArray()));

    public static Quiz Read(QuizAttempt attempt)
    {
        // Old attempts predate snapshots; retain their existing behavior until the next saved draft.
        if (attempt.QuizSnapshotJson is null)
            return attempt.QuizNavigation;
        var snapshot = JsonSerializer.Deserialize<AttemptQuizSnapshot>(attempt.QuizSnapshotJson)
                       ?? throw new InvalidOperationException("The attempt's quiz snapshot is invalid.");
        var quiz = new Quiz
        {
            Id = snapshot.Id, Title = snapshot.Title, Description = snapshot.Description,
            Duration = snapshot.Duration, Image = snapshot.Image, PassedScore = snapshot.PassedScore, IsActive = true
        };
        foreach (var saved in snapshot.Questions)
        {
            var question = new Question
            {
                Id = saved.Id, Content = saved.Content, Image = saved.Image, Level = saved.Level,
                QuestionType = saved.QuestionType, IsActive = true,
                Answers = saved.Answers.Select(answer => new Answer
                {
                    Id = answer.Id, QuestionId = saved.Id, Text = answer.Text,
                    IsCorrect = answer.IsCorrect, IsActive = true
                }).ToList()
            };
            quiz.QuizQuestions.Add(new QuizQuestion
            {
                QuizId = quiz.Id, QuestionId = saved.Id, Order = saved.Order,
                QuizNavigation = quiz, QuestionNavigation = question
            });
        }
        return quiz;
    }
}

internal sealed record AttemptQuestionSnapshot(Guid Id, string Content, string? Image, QuestionLevel? Level,
    QuestionType QuestionType, int Order, AttemptAnswerSnapshot[] Answers);

internal sealed record AttemptAnswerSnapshot(Guid Id, string Text, bool IsCorrect);
