using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.Common;

public sealed class UserProvisioning(IUserRepository users, IPasswordService passwords, TimeProvider clock)
{
    public async Task<User> CreateAsync(string username, string email, string password, UserProfileDto profile,
        bool isActive, CancellationToken cancellationToken)
    {
        username = username.Trim();
        email = email.Trim();
        await EnsureUniqueAsync(username, email, null, cancellationToken);
        var now = clock.GetUtcNow().UtcDateTime;
        var user = new User
        {
            Id = Guid.NewGuid(), Username = username, Email = email, Password = passwords.Hash(password),
            FullName = profile.FullName.Trim(), PhoneNumber = profile.PhoneNumber,
            DateOfBirth = profile.DateOfBirth, Avatar = profile.Avatar,
            IsActive = isActive, CreateAt = now, UpdateAt = now
        };
        users.Add(user);
        return user;
    }

    public async Task EnsureUniqueAsync(string username, string email, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await users.UsernameExistsAsync(username, excludedId, cancellationToken))
            throw new ConflictException(nameof(User), nameof(User.Username), username);
        if (await users.EmailExistsAsync(email, excludedId, cancellationToken))
            throw new ConflictException(nameof(User), nameof(User.Email), email);
    }
}
