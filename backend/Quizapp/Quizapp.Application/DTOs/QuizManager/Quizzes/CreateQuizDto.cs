namespace Quizapp.Application.DTOs.QuizManager.Quizzes;

public class CreateQuizDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Duration { get; set; }
    public string? Image { get; set; }
    public double PassedScore { get; set; }
    public bool IsActive { get; set; }
}
