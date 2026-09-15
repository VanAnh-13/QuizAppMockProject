using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.Questions;

public class CreateQuestionDto
{
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public bool IsActive { get; set; }
    public List<CreateQuestionAnswerDto> Answers { get; set; } = [];
}
