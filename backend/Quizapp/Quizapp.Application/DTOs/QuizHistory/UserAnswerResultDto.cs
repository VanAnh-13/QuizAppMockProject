using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizHistory;

public sealed class UserAnswerResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public IReadOnlyList<AnswerOptionDto> SelectedAnswers { get; set; } = [];
    public string? ResponseText { get; set; }
}
