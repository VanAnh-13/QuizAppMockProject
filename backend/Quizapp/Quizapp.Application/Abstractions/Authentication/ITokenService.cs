using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Authentication;

public interface ITokenService
{
    AccessToken Create(User user);
}

public sealed record AccessToken(string Value, DateTime ExpiresAt);
