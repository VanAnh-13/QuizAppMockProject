using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Infrastructure.Messaging;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(
        string to,
        string subject,
        string body,
        string? replyTo = null,
        CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host)
            || string.IsNullOrWhiteSpace(smtp.From)
            || string.IsNullOrWhiteSpace(smtp.UserName)
            || string.IsNullOrWhiteSpace(smtp.Password)
            || smtp.Port <= 0)
        {
            throw new QuizAppException("Email delivery is not configured.", "EMAIL_NOT_CONFIGURED");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        if (!string.IsNullOrWhiteSpace(replyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(replyTo));

        using var client = new SmtpClient();
        try
        {
            var secureSocketOptions = smtp.EnableSsl
                ? SecureSocketOptions.Auto
                : SecureSocketOptions.None;
            await client.ConnectAsync(smtp.Host, smtp.Port, secureSocketOptions, cancellationToken);
            await client.AuthenticateAsync(smtp.UserName, smtp.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (QuizAppException)
        {
            throw;
        }
        catch (Exception exception) when (exception is MailKit.Security.AuthenticationException
            or SmtpCommandException
            or SmtpProtocolException
            or SslHandshakeException
            or System.Net.Sockets.SocketException)
        {
            throw new QuizAppException("Could not send the message. Try again later.", exception, "EMAIL_SEND_FAILED");
        }
    }
}
