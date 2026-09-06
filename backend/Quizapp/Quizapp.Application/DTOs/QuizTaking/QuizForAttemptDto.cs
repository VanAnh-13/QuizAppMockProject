namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuizForAttemptDto
{
    public Guid QuizId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int Duration { get; init; }
    public string? Image { get; init; }
    public double? PassedScore { get; init; }
    public IReadOnlyList<QuestionForAttemptDto> Questions { get; init; } = [];
}
