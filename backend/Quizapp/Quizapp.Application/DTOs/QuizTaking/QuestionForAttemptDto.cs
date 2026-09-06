using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizTaking;

public sealed class QuestionForAttemptDto
{
    public Guid Id { get; init; }
    public required string Content { get; init; }
    public string? Image { get; init; }
    public QuestionLevel? Level { get; init; }
    public QuestionType QuestionType { get; init; }
    public int Order { get; init; }
    public IReadOnlyList<AnswerOptionDto> Answers { get; init; } = [];
}
