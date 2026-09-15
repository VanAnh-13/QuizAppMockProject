namespace Quizapp.Application.DTOs.UserManager;

public sealed class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserProfileDto Profile { get; set; } = new();
    public bool IsActive { get; set; }
}
