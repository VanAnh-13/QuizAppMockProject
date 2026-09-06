using System.Diagnostics.CodeAnalysis;
using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.DTOs.Authentication;

public class AuthResponseDto
{
    public required string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public required UserDto UserDto { get; set; }

    public AuthResponseDto()
    {
    }

    [SetsRequiredMembers]
    public AuthResponseDto(string token, DateTime expiresAt, UserDto userDto)
    {
        Token = token;
        ExpiresAt = expiresAt;
        UserDto = userDto;
    }
}
