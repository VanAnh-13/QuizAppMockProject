namespace Quizapp.Domain.Entities;

public class QuizAttempt
{
    public Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required Guid QuizId { get; init; }
    public required DateTime SubmitAt { get; init; }
    public required double Score { get; init; }

    public Quiz QuizNavigation { get; init; } = null!;
    public User UserNavigation { get; init; } = null!;
    public ICollection<UserAnswer> UserAnswers { get; init; } = new List<UserAnswer>();
}
