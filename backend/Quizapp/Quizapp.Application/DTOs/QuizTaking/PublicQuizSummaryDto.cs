namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class PublicQuizSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public double? PassedScore { get; set; }
    public int QuestionCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
