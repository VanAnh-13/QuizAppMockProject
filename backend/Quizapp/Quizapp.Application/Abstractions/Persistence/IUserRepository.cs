using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> TryIssuePasswordResetAsync(Guid userId, Guid securityStamp, string tokenHash,
        DateTimeOffset requestedAt, DateTimeOffset expiresAt, DateTimeOffset cooldownBefore,
        CancellationToken cancellationToken);

    Task<bool> TryResetPasswordAsync(Guid userId, Guid securityStamp, string tokenHash,
        string passwordHash, Guid newSecurityStamp, DateTimeOffset now, CancellationToken cancellationToken);

    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);

    Task<bool> UsernameExistsAsync(string username, Guid? excludedId, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, Guid? excludedId, CancellationToken cancellationToken);
}
