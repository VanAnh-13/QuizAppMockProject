namespace Quizapp.Domain.Entities;

public class QuizQuestion
{
    public const int FirstOrder = 1;

    public Guid Id { get; init; }
    public required Guid QuizId { get; init; }
    public required Guid QuestionId { get; init; }
    public int Order { get; set; } = FirstOrder;
    public Quiz QuizNavigation { get; init; } = null!;
    public Question QuestionNavigation { get; init; } = null!;
}
