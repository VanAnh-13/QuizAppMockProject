namespace Quizapp.Application.DTOs.UserManager;

public sealed class UserProfileDto
{
    public required string FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Avatar { get; init; }
}
