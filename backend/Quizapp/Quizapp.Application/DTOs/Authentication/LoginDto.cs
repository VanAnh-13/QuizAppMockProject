using System.Diagnostics.CodeAnalysis;

namespace Quizapp.Application.DTOs.Authentication;

public class LoginDto
{
    public required string Username { get; init; }
    public required string Password { get; init; }

    public LoginDto()
    {
    }

    [SetsRequiredMembers]
    public LoginDto(string username, string password)
    {
        Username = username;
        Password = password;
    }
}
