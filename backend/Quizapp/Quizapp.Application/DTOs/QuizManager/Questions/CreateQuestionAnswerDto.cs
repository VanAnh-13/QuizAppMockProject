namespace Quizapp.Application.DTOs.QuizManager.Questions;

public sealed class CreateQuestionAnswerDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; } = true;
}
