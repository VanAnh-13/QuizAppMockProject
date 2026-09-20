using System.Net;
using System.Net.Http.Json;
using Quizapp.Api.ExceptionHandlers;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.DTOs.Contact;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Tests.Api;

public class ContactApiTests
{
    [Fact]
    public async Task Valid_contact_message_returns_receipt_without_authentication()
    {
        var email = new RecordingEmailSender();
        await using var factory = new QuizappApiFactory { EmailSender = email };
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/public/contact", new
        {
            fullName = "Nguyen Van An",
            email = "an@example.com",
            subject = "Quiz feedback",
            message = "The Angular quiz helped me prepare for the exam."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<ContactReceiptDto>();
        Assert.NotNull(receipt);
        Assert.Equal(Assert.Single(factory.ContactMessages.Rows).Id, receipt.Id);
        Assert.True(receipt.ConfirmationEmailSent);
        Assert.Equal(2, email.Messages.Count);
        var sent = email.Messages[0];
        Assert.Equal("contact@levananh.dev", sent.To);
        Assert.Equal("an@example.com", sent.ReplyTo);
        Assert.Equal("an@example.com", email.Messages[1].To);
    }

    [Fact]
    public async Task Smtp_failure_returns_saved_receipt_without_claiming_confirmation_was_sent()
    {
        await using var factory = new QuizappApiFactory { EmailSender = new FailingEmailSender() };
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/public/contact", new
        {
            fullName = "Nguyen Van An", email = "an@example.com", message = "Please help."
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var receipt = await response.Content.ReadFromJsonAsync<ContactReceiptDto>();
        Assert.NotNull(receipt);
        Assert.Equal(Assert.Single(factory.ContactMessages.Rows).Id, receipt.Id);
        Assert.False(receipt.ConfirmationEmailSent);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("EMAIL_SEND_FAILED", json);
        Assert.DoesNotContain("an@example.com", json);
    }

    [Fact]
    public async Task Invalid_contact_message_returns_422()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/public/contact", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Empty(factory.ContactMessages.Rows);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error);
        Assert.NotNull(error.Errors);
        foreach (var field in new[] { "FullName", "Email", "Message" })
        {
            Assert.True(error.Errors.TryGetValue(field, out var messages), $"Missing validation errors for {field}.");
            Assert.NotEmpty(messages);
        }
    }

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body, string? replyTo = null,
            CancellationToken cancellationToken = default) =>
            throw new QuizAppException("SMTP authentication failed", "EMAIL_SEND_FAILED");
    }

    internal sealed class RecordingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body, string? ReplyTo)> Messages { get; } = [];

        public Task SendAsync(
            string to,
            string subject,
            string body,
            string? replyTo = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((to, subject, body, replyTo));
            return Task.CompletedTask;
        }
    }
}
