namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class CreateAnswerDto
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public bool IsActive { get; set; }
    public Guid QuestionId { get; set; }
}
