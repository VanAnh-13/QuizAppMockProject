namespace Quizapp.Domain.Entities;

public class Quiz
{
    public const double MinimumPassedScore = 0;

    public Guid Id { get; init; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required int Duration { get; set; }
    public string? Image { get; set; }
    public double? PassedScore { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreateAt { get; init; }
    public DateTime UpdateAt { get; set; }
    public ICollection<QuizAttempt> QuizAttempts { get; init; } = new List<QuizAttempt>();
    public ICollection<Question> Questions { get; init; } = new List<Question>();
    public ICollection<QuizQuestion> QuizQuestions { get; init; } = new List<QuizQuestion>();
}
