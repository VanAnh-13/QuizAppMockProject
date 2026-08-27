using QuizApp.Domain.Entities;

namespace QuizApp.Application.Interfaces;

/// <summary>
/// Issues signed JWT access tokens. Configuration-driven (key, issuer,
/// audience, lifetime) - never hardcoded.
/// </summary>
public interface IJwtTokenService
{
    (string Token, DateTimeOffset Expires) CreateToken(ApplicationUser user, IEnumerable<string> roles);
}
