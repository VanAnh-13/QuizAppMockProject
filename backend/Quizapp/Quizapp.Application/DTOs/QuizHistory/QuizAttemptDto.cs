namespace Quizapp.Application.DTOs.QuizHistory;

public class QuizAttemptDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public double Score { get; set; }
}
