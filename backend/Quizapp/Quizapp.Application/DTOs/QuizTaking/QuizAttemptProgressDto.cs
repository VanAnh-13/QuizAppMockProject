namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuizAttemptProgressDto : QuizAttemptStartDto
{
    public DateTime? PausedAt { get; set; }
    public DateTime? LastSavedAt { get; set; }
    public double RemainingSeconds { get; set; }
    public IReadOnlyList<SubmitAnswerDto> Answers { get; set; } = [];
}
