using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.DTOs.Authentication;

public class RegisterDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public UserProfileDto Profile { get; set; } = new();
}
