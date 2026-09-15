namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class UpdateAnswerDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }
}
