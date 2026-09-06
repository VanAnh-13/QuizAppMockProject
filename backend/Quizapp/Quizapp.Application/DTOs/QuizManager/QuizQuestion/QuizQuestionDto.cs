using System.Diagnostics.CodeAnalysis;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.QuizQuestion;

public class QuizQuestionDto
{
    public Guid Id { get; init; }
    public required Guid QuizId { get; init; }
    public required Guid QuestionId { get; init; }
    public required string Content { get; init; }
    public string? Image { get; init; }
    public QuestionLevel? Level { get; init; }
    public QuestionType QuestionType { get; set; }
    public int Order { get; init; }

    public QuizQuestionDto()
    {
    }

    [SetsRequiredMembers]
    public QuizQuestionDto(Guid id, Guid quizId, Guid questionId, QuestionType questionType, int order,
        string content, string? image = null, QuestionLevel? level = null)
    {
        Id = id;
        QuizId = quizId;
        QuestionId = questionId;
        QuestionType = questionType;
        Order = order;
        Content = content;
        Image = image;
        Level = level;
    }
}
