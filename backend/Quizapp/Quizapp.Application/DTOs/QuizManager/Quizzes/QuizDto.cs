using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.QuizManager.Quizzes;

public class QuizDto
{
    public Guid Id { get; set; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required int Duration { get; init; }
    public string? Image { get; init; }
    public double? PassedScore { get; init; }
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public QuizDto()
    {
    }

    [SetsRequiredMembers]
    public QuizDto(Guid id, string title, string? description, int duration, string? image, bool isActive,
        double? passedScore, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Duration = duration;
        Image = image;
        IsActive = isActive;
        PassedScore = passedScore;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
