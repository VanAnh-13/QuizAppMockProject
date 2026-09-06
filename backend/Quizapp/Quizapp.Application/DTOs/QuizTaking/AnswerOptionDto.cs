namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class AnswerOptionDto
{
    public Guid Id { get; init; }
    public required string Text { get; init; }
}
