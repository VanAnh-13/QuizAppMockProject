using System.Diagnostics.CodeAnalysis;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.DTOs.QuizManager.Questions;

public class UpdateQuestionDto
{
    public required string Content { get; init; }
    public string? Image { get; init; }
    public required QuestionLevel Level { get; init; }
    public QuestionType QuestionType { get; init; }
    public bool IsActive { get; init; }

    public UpdateQuestionDto()
    {
    }

    [SetsRequiredMembers]
    public UpdateQuestionDto(string content, QuestionType questionType, bool isActive,
        QuestionLevel level, string? image = null)
    {
        Content = content;
        QuestionType = questionType;
        IsActive = isActive;
        Image = image;
        Level = level;
    }
}
