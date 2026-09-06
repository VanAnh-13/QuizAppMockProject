using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.QuizManager.Answers;

public class AnswerDto
{
    public Guid Id { get; init; }
    public Guid QuestionId { get; set; }
    public required string Text { get; init; }
    public required bool IsCorrect { get; init; }
    public bool IsActive { get; init; }

    public AnswerDto()
    {
    }

    [SetsRequiredMembers]
    public AnswerDto(Guid id, Guid questionId, string text, bool isCorrect, bool isActive)
    {
        Id = id;
        QuestionId = questionId;
        Text = text;
        IsCorrect = isCorrect;
        IsActive = isActive;
    }
}
