using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.Authentication;

public sealed partial class AuthService
{
    public async Task RequestPasswordResetAsync(ForgotPasswordDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await forgotPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (string.IsNullOrWhiteSpace(resetOptions.Value.FrontendUrl))
            throw new QuizAppException("Password reset is temporarily unavailable. Please try again later.", "PASSWORD_RESET_BUSY");

        if (!resetQueue.TryEnqueue(new PasswordResetEmail(request.Email.Trim())))
            throw new QuizAppException("Password reset is busy. Please try again later.", "PASSWORD_RESET_BUSY");
    }

    public async Task SendPasswordResetEmailAsync(PasswordResetEmail message,
        CancellationToken cancellationToken = default)
    {
        if (message.IsConfirmation)
        {
            await emailSender.SendAsync(message.Email, "Your QuizApp password has been reset",
                "Your QuizApp password has been reset. All previous sessions have been signed out.\n\n"
                + "If you did not make this change, contact support immediately.\n\nQuizApp",
                cancellationToken: cancellationToken);
            return;
        }

        var user = await users.GetByEmailAsync(message.Email, cancellationToken);
        if (user is null || !user.IsActive)
            return;

        var now = clock.GetUtcNow();
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        if (!await users.TryIssuePasswordResetAsync(user.Id, user.SecurityStamp, HashResetToken(token), now,
                now.AddMinutes(PasswordResetOptions.TokenLifetimeMinutes),
                now.AddSeconds(-PasswordResetOptions.RequestCooldownSeconds), cancellationToken))
            return;

        var link = $"{resetOptions.Value.FrontendUrl}#userId={user.Id}&token={token}";
        await emailSender.SendAsync(user.Email, "Reset your QuizApp password",
            $"Use this link to choose a new password:\n\n{link}\n\n"
            + $"This link expires in {PasswordResetOptions.TokenLifetimeMinutes} minutes and can only be used once.\n"
            + "If you did not request this, you can ignore this email.\n\nQuizApp",
            cancellationToken: cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null || !user.IsActive
            || user.PasswordResetSecurityStamp != user.SecurityStamp
            || user.PasswordResetExpiresAt is null || user.PasswordResetExpiresAt <= clock.GetUtcNow()
            || user.PasswordResetTokenHash != HashResetToken(request.Token))
            throw InvalidResetLink();

        var email = user.Email;
        if (!await users.TryResetPasswordAsync(user.Id, user.SecurityStamp, HashResetToken(request.Token),
                passwords.Hash(request.NewPassword), Guid.NewGuid(), clock.GetUtcNow(), cancellationToken))
            throw InvalidResetLink();

        // A full notification queue must not undo an already completed password reset.
        resetQueue.TryEnqueue(new PasswordResetEmail(email, IsConfirmation: true));
    }

    private static string HashResetToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static BusinessRuleException InvalidResetLink() =>
        new("InvalidPasswordResetLink", "This password reset link is invalid or has expired. Request a new link.");
}
