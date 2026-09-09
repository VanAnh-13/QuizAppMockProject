namespace Quizapp.Domain.Entities;

public class UserAnswer
{
    public Guid Id { get; init; }
    public required Guid QuizAttemptId { get; init; }
    public required Guid QuestionId { get; init; }
    public Guid? AnswerId { get; init; }
    public string? ResponseText { get; init; }
    public QuizAttempt QuizAttemptNavigation { get; init; } = null!;
    public Question QuestionNavigation { get; init; } = null!;
    public Answer? AnswerNavigation { get; init; }
}
