using FluentValidation;
using Microsoft.Extensions.Options;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Contact;
using Quizapp.Application.Services.Contact;
using Quizapp.Application.Validators.Contact;
using Quizapp.Domain.Constants;
using Quizapp.Domain.Exceptions;
using Quizapp.Domain.Enums;
using ValidationException = FluentValidation.ValidationException;

namespace Quizapp.Tests.Application.Services;

public class ContactServiceTests
{
    private static readonly IOptions<ContactOptions> DefaultOptions =
        Options.Create(new ContactOptions { InboxAddress = "contact@levananh.dev" });

    private static ContactService CreateService(IEmailSender email) =>
        new(email, new ContactMessageDtoValidator(), DefaultOptions,
            new MemoryContactMessages(), new MemoryUnitOfWork(), TimeProvider.System);

    [Fact]
    public async Task Send_mails_the_inbox_and_sets_reply_to_the_submitter()
    {
        var email = new RecordingEmailSender();
        var service = CreateService(email);

        await service.SendAsync(new ContactMessageDto
        {
            FullName = " Nguyen Van An ",
            Email = " an@example.com ",
            Subject = " Quiz feedback ",
            Message = " The Angular quiz helped me prepare for the exam. "
        });

        Assert.Equal(2, email.Messages.Count);
        var sent = email.Messages[0];
        Assert.Equal("contact@levananh.dev", sent.To);
        Assert.Equal("Quiz feedback", sent.Subject);
        Assert.Equal("an@example.com", sent.ReplyTo);
        Assert.Contains("Nguyen Van An", sent.Body);
        Assert.Contains("The Angular quiz helped me prepare for the exam.", sent.Body);
        var confirmation = email.Messages[1];
        Assert.Equal("an@example.com", confirmation.To);
        Assert.Equal("contact@levananh.dev", confirmation.ReplyTo);
        Assert.Contains("We have received your feedback", confirmation.Body);
        Assert.DoesNotContain("The Angular quiz", confirmation.Body);
    }

    [Fact]
    public async Task Empty_subject_uses_a_default_and_still_reaches_the_inbox()
    {
        var email = new RecordingEmailSender();
        var service = CreateService(email);

        await service.SendAsync(new ContactMessageDto
        {
            FullName = "Nguyen Van An",
            Email = "an@example.com",
            Subject = "  ",
            Message = "Please help with my account."
        });

        Assert.Equal(2, email.Messages.Count);
        var sent = email.Messages[0];
        Assert.Equal("contact@levananh.dev", sent.To);
        Assert.Equal("QuizApp contact form", sent.Subject);
    }

    [Fact]
    public async Task Unconfigured_inbox_address_throws_exception_before_sending()
    {
        var email = new RecordingEmailSender();
        var service = new ContactService(
            email,
            new ContactMessageDtoValidator(),
            Options.Create(new ContactOptions { InboxAddress = "" }),
            new MemoryContactMessages(), new MemoryUnitOfWork(), TimeProvider.System);

        var exception = await Assert.ThrowsAsync<QuizAppException>(() => service.SendAsync(new ContactMessageDto
        {
            FullName = "Nguyen Van An",
            Email = "an@example.com",
            Subject = "Help",
            Message = "Please help with my account."
        }));

        Assert.Equal("CONTACT_NOT_CONFIGURED", exception.ErrorCode);
        Assert.Empty(email.Messages);
    }

    [Fact]
    public async Task Invalid_messages_are_rejected_before_mail_is_sent()
    {
        var email = new RecordingEmailSender();
        var service = CreateService(email);

        await Assert.ThrowsAsync<ValidationException>(() => service.SendAsync(new ContactMessageDto()));
        Assert.Empty(email.Messages);
    }

    [Fact]
    public async Task Overlong_fields_are_rejected()
    {
        var email = new RecordingEmailSender();
        var service = CreateService(email);

        await Assert.ThrowsAsync<ValidationException>(() => service.SendAsync(new ContactMessageDto
        {
            FullName = new string('A', FieldLimits.FullNameLength + 1),
            Email = "an@example.com",
            Subject = new string('S', FieldLimits.QuizTitleLength + 1),
            Message = new string('M', FieldLimits.ContentLength + 1)
        }));
        Assert.Empty(email.Messages);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Records_feedback_before_sending_and_tracks_independent_delivery_results(
        bool failNotification, bool failConfirmation)
    {
        var messages = new MemoryContactMessages();
        var work = new RecordingUnitOfWork();
        var email = new RecordingEmailSender
        {
            BeforeSend = to =>
            {
                Assert.True(work.Saves > 0);
                var saved = Assert.Single(messages.Rows);
                Assert.Equal("Nguyen Van An", saved.FullName);
                Assert.Equal("an@example.com", saved.Email);
                Assert.Equal("Help", saved.Subject);
                Assert.Equal("Please help.", saved.Message);
                if (to == "an@example.com" ? failConfirmation : failNotification)
                    throw new QuizAppException("Authentication failed", "EMAIL_SEND_FAILED");
            }
        };
        var service = new ContactService(email, new ContactMessageDtoValidator(), DefaultOptions,
            messages, work, TimeProvider.System);

        var receipt = await service.SendAsync(new ContactMessageDto
        {
            FullName = " Nguyen Van An ", Email = " an@example.com ",
            Subject = " Help ", Message = " Please help. "
        });

        var record = Assert.Single(messages.Rows);
        Assert.Equal(record.Id, receipt.Id);
        Assert.Equal(record.ReceivedAt, receipt.ReceivedAt);
        Assert.NotEqual(Guid.Empty, receipt.Id);
        Assert.Equal(!failConfirmation, receipt.ConfirmationEmailSent);
        Assert.Equal(failNotification ? ContactEmailStatus.Failed : ContactEmailStatus.Sent, record.NotificationStatus);
        Assert.Equal(failConfirmation ? ContactEmailStatus.Failed : ContactEmailStatus.Sent, record.ConfirmationStatus);
        Assert.Equal(failNotification ? "EMAIL_SEND_FAILED" : null, record.NotificationErrorCode);
        Assert.Equal(failConfirmation ? "EMAIL_SEND_FAILED" : null, record.ConfirmationErrorCode);
        Assert.Equal(3, work.Saves);
        Assert.Equal(2, email.Messages.Count);
    }

    [Fact]
    public async Task Database_failure_prevents_any_email_or_success_receipt()
    {
        var email = new RecordingEmailSender();
        var service = new ContactService(email, new ContactMessageDtoValidator(), DefaultOptions,
            new MemoryContactMessages(), new RecordingUnitOfWork { Fail = true }, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync(new ContactMessageDto
        {
            FullName = "Name", Email = "an@example.com", Message = "Please help."
        }));
        Assert.Empty(email.Messages);
    }

    [Fact]
    public async Task Cancellation_is_not_reported_as_a_successful_delivery()
    {
        using var cancellation = new CancellationTokenSource();
        var messages = new MemoryContactMessages();
        var email = new RecordingEmailSender
        {
            BeforeSend = _ =>
            {
                cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }
        };
        var service = new ContactService(email, new ContactMessageDtoValidator(), DefaultOptions,
            messages, new RecordingUnitOfWork(), TimeProvider.System);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SendAsync(new ContactMessageDto
        {
            FullName = "Name", Email = "an@example.com", Message = "Please help."
        }, cancellation.Token));
        Assert.Equal(ContactEmailStatus.Pending, Assert.Single(messages.Rows).ConfirmationStatus);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int Saves { get; private set; }
        public bool Fail { get; init; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Fail) throw new InvalidOperationException("Database unavailable");
            Saves++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public Action<string>? BeforeSend { get; init; }
        public List<(string To, string Subject, string Body, string? ReplyTo)> Messages { get; } = [];

        public Task SendAsync(
            string to,
            string subject,
            string body,
            string? replyTo = null,
            CancellationToken cancellationToken = default)
        {
            Messages.Add((to, subject, body, replyTo));
            BeforeSend?.Invoke(to);
            return Task.CompletedTask;
        }
    }
}
