namespace QuizApp.Domain.Entities;

/// <summary>
/// Contact-form submissions (stored + logged, no SMTP).
/// </summary>
public class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
