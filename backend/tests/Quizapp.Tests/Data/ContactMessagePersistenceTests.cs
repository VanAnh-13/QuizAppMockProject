using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.DTOs.Contact;
using Quizapp.Application.Services.Contact;
using Quizapp.Application.Validators.Contact;
using Quizapp.Domain.Constants;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Tests.Data;

public class ContactMessagePersistenceTests(SqlServerFixture fixture) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task Smtp_failure_preserves_all_fields_and_delivery_results_in_sql_server()
    {
        await using var db = fixture.CreateContext();
        var service = new ContactService(new FailingEmailSender(), new ContactMessageDtoValidator(),
            Options.Create(new ContactOptions { InboxAddress = "support@example.com" }),
            new EfContactMessageRepository(db), new EfUnitOfWork(db), TimeProvider.System);

        var receipt = await service.SendAsync(new ContactMessageDto
        {
            FullName = " Nguyễn Văn An ", Email = " an@example.com ",
            Subject = " Hỗ trợ ", Message = " Cần hỗ trợ tài khoản. "
        });

        await using var reloaded = fixture.CreateContext();
        var saved = await reloaded.ContactMessages.SingleAsync(message => message.Id == receipt.Id);
        Assert.Equal("Nguyễn Văn An", saved.FullName);
        Assert.Equal("an@example.com", saved.Email);
        Assert.Equal("Hỗ trợ", saved.Subject);
        Assert.Equal("Cần hỗ trợ tài khoản.", saved.Message);
        Assert.Equal(receipt.ReceivedAt, saved.ReceivedAt);
        Assert.Equal(ContactEmailStatus.Failed, saved.NotificationStatus);
        Assert.Equal(ContactEmailStatus.Failed, saved.ConfirmationStatus);
        Assert.Equal("EMAIL_SEND_FAILED", saved.NotificationErrorCode);
        Assert.Equal("EMAIL_SEND_FAILED", saved.ConfirmationErrorCode);
        Assert.False(receipt.ConfirmationEmailSent);
    }

    [SqlServerFact]
    public async Task Database_rejects_overlong_messages_and_unknown_delivery_status()
    {
        await using var db = fixture.CreateContext();
        db.ContactMessages.Add(new ContactMessage
        {
            Id = Guid.NewGuid(), FullName = "Name", Email = "an@example.com", Subject = "Help",
            Message = new string('M', FieldLimits.ContentLength + 1), ReceivedAt = DateTimeOffset.UtcNow
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.ContactMessages.Add(new ContactMessage
        {
            Id = Guid.NewGuid(), FullName = "Name", Email = "an@example.com", Subject = "Help",
            Message = "Help", ReceivedAt = DateTimeOffset.UtcNow, ConfirmationStatus = (ContactEmailStatus)999
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(string to, string subject, string body, string? replyTo = null,
            CancellationToken cancellationToken = default) =>
            throw new QuizAppException("SMTP authentication failed", "EMAIL_SEND_FAILED");
    }
}
