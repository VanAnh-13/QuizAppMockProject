namespace Quizapp.Application.DTOs.UserManager;

public sealed class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserProfileDto Profile { get; set; } = new();
    public bool IsActive { get; set; }
}
