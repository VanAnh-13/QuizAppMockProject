namespace Quizapp.Application.DTOs.Contact;

public sealed class ContactReceiptDto
{
    public Guid Id { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public bool ConfirmationEmailSent { get; init; }
}
