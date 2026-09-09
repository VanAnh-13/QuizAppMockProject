using System.Text;

namespace Quizapp.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string SecurityStampClaim = "security_stamp";
    public const string RoleClaim = "role";
    private const int DefaultLifetimeMinutes = 60;
    private const int MinimumSigningKeyBytes = 32;
    private const int MaximumLifetimeMinutes = 1440;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeMinutes { get; init; } = DefaultLifetimeMinutes;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience)
                                              || string.IsNullOrWhiteSpace(SigningKey) ||
                                              Encoding.UTF8.GetByteCount(SigningKey) < MinimumSigningKeyBytes
                                              || LifetimeMinutes is < 1 or > MaximumLifetimeMinutes)
            throw new InvalidOperationException(
                "Configure Jwt issuer, audience, a signing key of at least 32 UTF-8 bytes, and lifetime of 1-1440 minutes.");
    }
}
