using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizHistory;

public sealed class UserAnswerResultDto
{
    public Guid QuestionId { get; init; }
    public required string QuestionContent { get; init; }
    public QuestionType QuestionType { get; init; }
    public string? Image { get; init; }
    public QuestionLevel? Level { get; init; }
    public IReadOnlyList<AnswerOptionDto> SelectedAnswers { get; init; } = [];
    public string? ResponseText { get; init; }
}
