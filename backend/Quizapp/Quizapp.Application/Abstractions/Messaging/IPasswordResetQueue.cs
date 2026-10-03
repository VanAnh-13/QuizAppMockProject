namespace Quizapp.Application.Abstractions.Messaging;

public sealed record PasswordResetEmail(string Email, bool IsConfirmation = false);

public interface IPasswordResetQueue
{
    bool TryEnqueue(PasswordResetEmail message);
}
