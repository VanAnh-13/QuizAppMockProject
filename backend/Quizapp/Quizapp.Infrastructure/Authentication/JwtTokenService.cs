using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    public AccessToken Create(User user)
    {
        var settings = options.Value;
        settings.Validate();
        var now = clock.GetUtcNow().UtcDateTime;
        var expiresAt = now.AddMinutes(settings.LifetimeMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtOptions.SecurityStampClaim, user.SecurityStamp.ToString())
        };
        claims.AddRange(user.Roles.Where(role => role.IsActive)
            .Select(role => new Claim(JwtOptions.RoleClaim, role.RoleName)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            notBefore: now, expires: expiresAt, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
