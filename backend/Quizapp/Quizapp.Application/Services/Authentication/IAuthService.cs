using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Abstractions.Messaging;

namespace Quizapp.Application.Services.Authentication;

public interface IAuthService
{
    Task RequestPasswordResetAsync(
        ForgotPasswordDto request,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        ResetPasswordDto request,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetEmailAsync(
        PasswordResetEmail message,
        CancellationToken cancellationToken = default);

    Task<UserDto> RegisterAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default);

    Task<AuthResponseDto> LoginAsync(
        LoginDto request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        ChangePasswordDto request,
        CancellationToken cancellationToken = default);

    Task<UserDto> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);
}
