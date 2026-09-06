namespace Quizapp.Application.DTOs.QuizHistory;

public class QuizAttemptDto
{
    public Guid Id { get; init; }
    public Guid QuizId { get; init; }
    public required string QuizTitle { get; init; }
    public DateTime SubmittedAt { get; init; }
    public double Score { get; init; }
}
