namespace Quizapp.Application.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendAsync(
        string to,
        string subject,
        string body,
        string? replyTo = null,
        CancellationToken cancellationToken = default);
}
