namespace QuizApp.Domain.Entities;

public class Answer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Content { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid QuestionId { get; set; }

    public Question Question { get; set; } = null!;
}
