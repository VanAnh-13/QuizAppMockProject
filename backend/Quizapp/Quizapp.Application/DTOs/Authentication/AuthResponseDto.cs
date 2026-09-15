using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.DTOs.Authentication;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto UserDto { get; set; } = new();
}
