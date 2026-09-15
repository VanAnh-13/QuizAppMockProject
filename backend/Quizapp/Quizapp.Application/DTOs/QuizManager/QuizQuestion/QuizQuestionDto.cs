using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.QuizQuestion;

public class QuizQuestionDto
{
    public Guid Id { get; set; }
    public Guid QuizId { get; set; }
    public Guid QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public int Order { get; set; }
}
