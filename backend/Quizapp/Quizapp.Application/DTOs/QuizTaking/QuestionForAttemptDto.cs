using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuestionForAttemptDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Image { get; set; }
    public QuestionLevel? Level { get; set; }
    public QuestionType QuestionType { get; set; }
    public int Order { get; set; }
    public IReadOnlyList<AnswerOptionDto> Answers { get; set; } = [];
}
