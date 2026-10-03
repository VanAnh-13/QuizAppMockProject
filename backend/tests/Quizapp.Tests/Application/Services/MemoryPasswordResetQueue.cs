using Quizapp.Application.Abstractions.Messaging;

namespace Quizapp.Tests.Application.Services;

internal sealed class MemoryPasswordResetQueue : IPasswordResetQueue
{
    public List<PasswordResetEmail> Messages { get; } = [];

    public bool TryEnqueue(PasswordResetEmail message)
    {
        Messages.Add(message);
        return true;
    }
}
