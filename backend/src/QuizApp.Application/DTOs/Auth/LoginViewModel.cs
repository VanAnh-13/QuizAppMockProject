namespace QuizApp.Application.DTOs.Auth;

/// <summary>Matches LoginViewModel from the handout.</summary>
public class LoginViewModel
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
