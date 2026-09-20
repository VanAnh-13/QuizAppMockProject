using Quizapp.Application.DTOs.Contact;

namespace Quizapp.Application.Services.Contact;

public interface IContactService
{
    Task<ContactReceiptDto> SendAsync(ContactMessageDto request, CancellationToken cancellationToken = default);
}
