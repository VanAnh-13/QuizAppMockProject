using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.Services.Authentication;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(
        LoginDto request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);
}