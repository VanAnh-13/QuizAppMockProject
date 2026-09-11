namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuizAttemptSummaryDto
{
    public Guid AttemptId { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public DateTime ServerTime { get; set; }
    public double RemainingSeconds { get; set; }
    public long Revision { get; set; }
}
