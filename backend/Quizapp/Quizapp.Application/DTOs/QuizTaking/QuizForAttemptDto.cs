namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuizForAttemptDto
{
    public Guid QuizId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public string? Image { get; set; }
    public double? PassedScore { get; set; }
    public IReadOnlyList<QuestionForAttemptDto> Questions { get; set; } = [];
}
