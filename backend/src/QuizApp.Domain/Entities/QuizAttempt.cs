using QuizApp.Domain.Enums;

namespace QuizApp.Domain.Entities;

public class QuizAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid QuizId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Snapshot of the code that started this attempt.</summary>
    public string? QuizCode { get; set; }

    /// <summary>Recorded server-side when the attempt starts.</summary>
    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public QuizAttemptStatus Status { get; set; } = QuizAttemptStatus.Prepared;

    /// <summary>Percentage 0-100, computed server-side on submission.</summary>
    public int? Score { get; set; }

    public int? CorrectCount { get; set; }

    public int? TotalQuestions { get; set; }

    /// <summary>Deadline = StartTime + quiz duration. Snapshotted on take.</summary>
    public DateTimeOffset? Deadline { get; set; }

    // Navigation properties
    public Quiz Quiz { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;

    public ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
