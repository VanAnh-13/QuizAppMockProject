using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class CreateAnswerDto
{
    public required string Text { get; init; }
    public required bool IsCorrect { get; init; }
    public bool IsActive { get; init; }
    public Guid QuestionId { get; set; }

    public CreateAnswerDto()
    {
    }

    [SetsRequiredMembers]
    public CreateAnswerDto(string text, bool isCorrect, bool isActive, Guid questionId)
    {
        Text = text;
        IsCorrect = isCorrect;
        IsActive = isActive;
        QuestionId = questionId;
    }
}
