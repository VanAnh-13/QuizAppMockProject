namespace Quizapp.Application.DTOs.QuizTaking;

public class QuizAttemptStartDto
{
    public Guid AttemptId { get; set; }
    public QuizForAttemptDto Quiz { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}