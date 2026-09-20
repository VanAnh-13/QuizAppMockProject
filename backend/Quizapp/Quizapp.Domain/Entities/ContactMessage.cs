using Quizapp.Domain.Enums;

namespace Quizapp.Domain.Entities;

public sealed class ContactMessage
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public ContactEmailStatus NotificationStatus { get; set; }
    public ContactEmailStatus ConfirmationStatus { get; set; }
    public string? NotificationErrorCode { get; set; }
    public string? ConfirmationErrorCode { get; set; }
}
