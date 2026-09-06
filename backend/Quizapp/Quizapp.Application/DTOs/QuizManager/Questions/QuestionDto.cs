using System.Diagnostics.CodeAnalysis;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.Questions;

public class QuestionDto
{
    public Guid Id { get; init; }
    public required string Content { get; init; }
    public string? Image { get; init; }
    public QuestionLevel? Level { get; init; }
    public QuestionType QuestionType { get; init; }
    public bool IsActive { get; init; }

    public QuestionDto()
    {
    }

    [SetsRequiredMembers]
    public QuestionDto(Guid id, string content, QuestionType questionType, bool isActive,
        QuestionLevel? level, string? image = null)
    {
        Id = id;
        Content = content;
        QuestionType = questionType;
        IsActive = isActive;
        Image = image;
        Level = level;
    }
}
