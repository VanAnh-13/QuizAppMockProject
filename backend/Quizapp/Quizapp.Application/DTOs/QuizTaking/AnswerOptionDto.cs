namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class AnswerOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
