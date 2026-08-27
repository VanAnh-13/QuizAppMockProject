using QuizApp.Application.DTOs.Auth;
using QuizApp.Application.DTOs.Users;

namespace QuizApp.Application.Interfaces;

public interface IAuthService
{
    Task<UserViewModel> RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken = default);

    Task<LoginResponseViewModel> LoginAsync(LoginViewModel model, CancellationToken cancellationToken = default);

    Task<UserViewModel> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordViewModel model, CancellationToken cancellationToken = default);

    Task<UserViewModel> UpdateProfileAsync(Guid userId, UpdateProfileViewModel model, CancellationToken cancellationToken = default);

    /// <summary>Persists the avatar URL produced by an upload onto the user.</summary>
    Task<string> SetAvatarAsync(Guid userId, string avatarUrl, CancellationToken cancellationToken = default);
}
