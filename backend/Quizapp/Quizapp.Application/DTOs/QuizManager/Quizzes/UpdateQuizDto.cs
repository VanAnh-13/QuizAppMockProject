using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.QuizManager.Quizzes;

public class UpdateQuizDto
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required int Duration { get; init; }
    public string? Image { get; init; }
    public required double PassedScore { get; init; }
    public bool IsActive { get; set; }

    public UpdateQuizDto()
    {
    }

    [SetsRequiredMembers]
    public UpdateQuizDto(string title, string? description, int duration, string? image, bool isActive,
        double passedScore)
    {
        Title = title;
        Description = description;
        Duration = duration;
        Image = image;
        IsActive = isActive;
        PassedScore = passedScore;
    }
}
