using System.Diagnostics.CodeAnalysis;
using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.DTOs.Authentication;

public class RegisterDto
{
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string ConfirmPassword { get; init; }
    public required UserProfileDto Profile { get; init; }

    public RegisterDto()
    {
    }

    [SetsRequiredMembers]
    public RegisterDto(string username, string email, string password, string confirmPassword, UserProfileDto profile)
    {
        Username = username;
        Email = email;
        Password = password;
        ConfirmPassword = confirmPassword;
        Profile = profile;
    }
}
