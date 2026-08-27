using QuizApp.Application.DTOs.Users;

namespace QuizApp.Application.DTOs.Auth;

/// <summary>Matches LoginResponseViewModel from the handout.</summary>
public class LoginResponseViewModel
{
    public UserViewModel UserInformation { get; set; } = new();

    public string Token { get; set; } = string.Empty;

    public DateTimeOffset Expires { get; set; }
}
