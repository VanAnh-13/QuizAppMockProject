using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class UpdateAnswerDto
{
    public required string Text { get; init; }
    public required bool IsCorrect { get; init; }
    public bool IsActive { get; init; }

    public UpdateAnswerDto()
    {
    }

    [SetsRequiredMembers]
    public UpdateAnswerDto(string text, bool isCorrect, bool isActive)
    {
        Text = text;
        IsCorrect = isCorrect;
        IsActive = isActive;
    }
}
