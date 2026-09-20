using FluentValidation;
using Microsoft.Extensions.Options;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Contact;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.Contact;

public sealed class ContactService(
    IEmailSender email,
    IValidator<ContactMessageDto> validator,
    IOptions<ContactOptions> options,
    IContactMessageRepository messages,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IContactService
{
    public async Task<ContactReceiptDto> SendAsync(ContactMessageDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var inboxAddress = options.Value.InboxAddress;
        if (string.IsNullOrWhiteSpace(inboxAddress))
        {
            throw new QuizAppException("Contact inbox is not configured.", "CONTACT_NOT_CONFIGURED");
        }

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? "QuizApp contact form"
            : request.Subject.Trim();

        var message = new ContactMessage
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Subject = subject,
            Message = request.Message.Trim(),
            ReceivedAt = clock.GetUtcNow()
        };
        messages.Add(message);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var body = $"Name: {request.FullName.Trim()}\nEmail: {request.Email.Trim()}\nSubject: {subject}\n\n{request.Message.Trim()}";

        message.NotificationErrorCode = await TrySendAsync(
            inboxAddress.Trim(),
            subject,
            body,
            message.Email,
            cancellationToken);
        message.NotificationStatus = message.NotificationErrorCode is null
            ? ContactEmailStatus.Sent
            : ContactEmailStatus.Failed;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var confirmation = $"We have received your feedback.\n\nReference: {message.Id}\n\n"
            + "Your message has been saved. Our team will review it and reply to this email address.\n\n"
            + "If you did not submit this form, you can ignore this email.\n\nQuizApp";
        message.ConfirmationErrorCode = await TrySendAsync(
            message.Email,
            "QuizApp has received your feedback",
            confirmation,
            inboxAddress.Trim(),
            cancellationToken);
        message.ConfirmationStatus = message.ConfirmationErrorCode is null
            ? ContactEmailStatus.Sent
            : ContactEmailStatus.Failed;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ContactReceiptDto
        {
            Id = message.Id,
            ReceivedAt = message.ReceivedAt,
            ConfirmationEmailSent = message.ConfirmationStatus == ContactEmailStatus.Sent
        };
    }

    private async Task<string?> TrySendAsync(
        string to, string subject, string body, string replyTo, CancellationToken cancellationToken)
    {
        try
        {
            await email.SendAsync(to, subject, body, replyTo, cancellationToken);
            return null;
        }
        catch (QuizAppException exception) when (exception.ErrorCode is "EMAIL_SEND_FAILED" or "EMAIL_NOT_CONFIGURED")
        {
            return exception.ErrorCode;
        }
    }
}
