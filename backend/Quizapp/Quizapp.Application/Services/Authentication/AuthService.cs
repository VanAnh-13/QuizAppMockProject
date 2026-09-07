using FluentValidation;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.Authentication;

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    UserProvisioning provisioning,
    IPasswordService passwords,
    ITokenService tokens,
    ServiceAuthorization authorization,
    TimeProvider clock,
    IValidator<RegisterDto> registerValidator,
    IValidator<LoginDto> loginValidator,
    IValidator<ChangePasswordDto> passwordValidator) : IAuthService
{
    public async Task<UserDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await provisioning.CreateAsync(request.Username, request.Email, request.Password,
            request.Profile, true, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await users.GetByUsernameAsync(request.Username.Trim(), cancellationToken);

        if (user is null || !user.IsActive || !passwords.Verify(user.Password, request.Password))
            throw new AuthenticationException("Invalid username or password.");

        cancellationToken.ThrowIfCancellationRequested();
        var token = tokens.Create(user);

        return new AuthResponseDto
            { Token = token.Value, ExpiresAt = token.ExpiresAt, UserDto = DtoMapping.ToDto(user) };
    }

    public async Task ChangePasswordAsync(ChangePasswordDto request, CancellationToken cancellationToken = default)
    {
        var user = await authorization.RequireUserAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await passwordValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!passwords.Verify(user.Password, request.CurrentPassword))
            throw new AuthenticationException("The current password is incorrect.");

        user.Password = passwords.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid();

        user.UpdateAt = clock.GetUtcNow()
            .UtcDateTime;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
