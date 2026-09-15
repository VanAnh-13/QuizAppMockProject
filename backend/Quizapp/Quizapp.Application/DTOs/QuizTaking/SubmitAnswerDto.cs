namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class SubmitAnswerDto
{
    public Guid QuestionId { get; set; }
    public List<Guid> AnswerIds { get; set; } = [];
    public string? ResponseText { get; set; }
}
