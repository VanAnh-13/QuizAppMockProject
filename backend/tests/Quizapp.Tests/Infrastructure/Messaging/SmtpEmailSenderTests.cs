using Microsoft.Extensions.Options;
using Quizapp.Domain.Exceptions;
using Quizapp.Infrastructure.Messaging;

namespace Quizapp.Tests.Infrastructure.Messaging;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task Missing_credentials_fail_before_connecting()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            Host = "smtppro.zoho.com",
            Port = 587,
            EnableSsl = true,
            From = "contact@levananh.dev",
            UserName = "",
            Password = ""
        }));

        var exception = await Assert.ThrowsAsync<QuizAppException>(() =>
            sender.SendAsync("contact@levananh.dev", "Subject", "Body"));

        Assert.Equal("EMAIL_NOT_CONFIGURED", exception.ErrorCode);
    }

    [Fact]
    public async Task Missing_or_invalid_port_fails_before_connecting()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            Host = "smtppro.zoho.com",
            Port = 0,
            EnableSsl = true,
            From = "contact@levananh.dev",
            UserName = "username",
            Password = "password"
        }));

        var exception = await Assert.ThrowsAsync<QuizAppException>(() =>
            sender.SendAsync("contact@levananh.dev", "Subject", "Body"));

        Assert.Equal("EMAIL_NOT_CONFIGURED", exception.ErrorCode);
    }
}
