namespace Quizapp.Application.DTOs.UserManager;

public sealed class UserProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Avatar { get; set; }
}
