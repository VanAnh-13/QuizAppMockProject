namespace QuizApp.Domain.Entities;

/// <summary>
/// Join table binding a question-bank question to a quiz with a display order.
/// A question can serve many quizzes.
/// </summary>
public class QuizQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizId { get; set; }

    public Guid QuestionId { get; set; }

    public int DisplayOrder { get; set; }

    public Quiz Quiz { get; set; } = null!;

    public Question Question { get; set; } = null!;
}
