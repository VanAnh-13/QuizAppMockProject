using Microsoft.AspNetCore.Identity;
using Quizapp.Application.Abstractions.Authentication;

namespace Quizapp.Infrastructure.Authentication;

public sealed class IdentityPasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _subject = new();

    public string Hash(string password) => _hasher.HashPassword(_subject, password);

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(password))
            return false;
        try
        {
            return _hasher.VerifyHashedPassword(_subject, hash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Legacy plaintext or malformed stored values are not valid password hashes.
            return false;
        }
    }
}
