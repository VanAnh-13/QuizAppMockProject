namespace QuizApp.Domain.Entities;

/// <summary>
/// A quiz access code bound to (quiz, user). Start buttons self-issue one,
/// admins can bulk-generate them, and the /quizzes code box consumes them.
/// </summary>
public class QuizCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public Guid QuizId { get; set; }

    public Guid UserId { get; set; }

    public bool IsUsed { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Quiz Quiz { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;
}
