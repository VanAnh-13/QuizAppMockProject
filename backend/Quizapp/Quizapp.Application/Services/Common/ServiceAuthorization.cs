using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.Common;

public sealed class ServiceAuthorization(ICurrentUser currentUser, IUserRepository users)
{
    public const string AdministratorRole = "Admin";

    public async Task<User> RequireUserAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (currentUser.UserId is not { } id)
            throw new AuthenticationException("Sign in before performing this operation.");

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AuthenticationException("The account is unavailable.");
        return user;
    }

    public async Task<User> RequireAdminAsync(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (!IsAdmin(user))
            throw new ForbiddenException("An active administrator role is required.");
        return user;
    }

    public static bool IsAdmin(User user) => user.Roles.Any(role =>
        role.IsActive && string.Equals(role.RoleName, AdministratorRole, StringComparison.OrdinalIgnoreCase));
}
