namespace Quizapp.Domain.Entities;

public class Answer
{
    public Guid Id { get; init; }
    public required string Text { get; init; }
    public required bool IsCorrect { get; init; }
    public bool IsActive { get; init; }
    public ICollection<UserAnswer> UserAnswers { get; init; } = new List<UserAnswer>();
    public Question QuestionNavigation { get; init; } = null!;
    public Guid QuestionId { get; init; }
}
