using QuizApp.Domain.Enums;

namespace QuizApp.Domain.Entities;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Content { get; set; } = string.Empty;

    public QuestionType QuestionType { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
}
