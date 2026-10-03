using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfUserRepository(QuizAppDbContext db) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Email.ToLower() == email.ToLower(), cancellationToken);

    public async Task<bool> TryIssuePasswordResetAsync(Guid userId, Guid securityStamp, string tokenHash,
        DateTimeOffset requestedAt, DateTimeOffset expiresAt, DateTimeOffset cooldownBefore,
        CancellationToken cancellationToken) =>
        await db.Users.Where(user => user.Id == userId && user.Status == UserStatus.Active
                && user.SecurityStamp == securityStamp
                && (user.PasswordResetRequestedAt == null || user.PasswordResetRequestedAt <= cooldownBefore))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.PasswordResetTokenHash, tokenHash)
                .SetProperty(user => user.PasswordResetSecurityStamp, securityStamp)
                .SetProperty(user => user.PasswordResetRequestedAt, requestedAt)
                .SetProperty(user => user.PasswordResetExpiresAt, expiresAt), cancellationToken) == 1;

    public async Task<bool> TryResetPasswordAsync(Guid userId, Guid securityStamp, string tokenHash,
        string passwordHash, Guid newSecurityStamp, DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.Users.Where(user => user.Id == userId && user.Status == UserStatus.Active
                && user.SecurityStamp == securityStamp && user.PasswordResetSecurityStamp == securityStamp
                && user.PasswordResetTokenHash == tokenHash && user.PasswordResetExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.Password, passwordHash)
                .SetProperty(user => user.SecurityStamp, newSecurityStamp)
                .SetProperty(user => user.UpdateAt, now.UtcDateTime)
                .SetProperty(user => user.PasswordResetTokenHash, (string?)null)
                .SetProperty(user => user.PasswordResetExpiresAt, (DateTimeOffset?)null)
                .SetProperty(user => user.PasswordResetSecurityStamp, (Guid?)null), cancellationToken) == 1;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);

    public async Task<PagedResultDto<User>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query.Include(u => u.Roles)
            .OrderBy(u => u.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<User>
            { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public void Add(User entity) => db.Users.Add(entity);

    public void Remove(User entity) => db.Users.Remove(entity);

    public Task<bool> UsernameExistsAsync(string username, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() && u.Id != excludedId, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != excludedId, cancellationToken);
}
