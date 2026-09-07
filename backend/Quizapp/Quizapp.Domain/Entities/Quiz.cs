namespace Quizapp.Domain.Entities;

public class Quiz
{
    public const double MinimumPassedScore = 0;

    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required int Duration { get; init; }
    public string? Image { get; init; }
    public double? PassedScore { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreateAt { get; init; }
    public DateTime UpdateAt { get; init; }

    public ICollection<QuizAttempt> QuizAttempts { get; init; } = new List<QuizAttempt>();
    public ICollection<Question> Questions { get; init; } = new List<Question>();
    public ICollection<QuizQuestion> QuizQuestions { get; init; } = new List<QuizQuestion>();
}
