using QuizApp.Domain.Enums;

namespace QuizApp.Domain.Entities;

public class Quiz
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Duration in minutes.</summary>
    public int Duration { get; set; }

    public string? ThumbnailUrl { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    // Navigation properties
    public ICollection<QuizQuestion> QuizQuestions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    public ICollection<QuizCode> Codes { get; set; } = new List<QuizCode>();
}
