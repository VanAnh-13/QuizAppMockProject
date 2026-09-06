using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.QuizManager.Quizzes;

public class CreateQuizDto
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required int Duration { get; init; }
    public string? Image { get; init; }
    public required double PassedScore { get; init; }
    public bool IsActive { get; set; }

    public CreateQuizDto()
    {
    }

    [SetsRequiredMembers]
    public CreateQuizDto(string title, string? description, int duration, string? image, bool isActive,
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
