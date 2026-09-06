namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SubmitAnswerDto
{
    public required Guid QuestionId { get; init; }
    public List<Guid> AnswerIds { get; init; } = [];
    public string? ResponseText { get; init; }
}
