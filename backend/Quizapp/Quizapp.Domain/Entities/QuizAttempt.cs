namespace Quizapp.Domain.Entities;

public class QuizAttempt
{
    public Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid QuizId { get; init; }
    public required DateTime StartedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public DateTime? SubmitAt { get; set; }
    public double Score { get; set; }
    public Quiz QuizNavigation { get; init; } = null!;
    public User UserNavigation { get; init; } = null!;
    public ICollection<UserAnswer> UserAnswers { get; init; } = new List<UserAnswer>();
}
