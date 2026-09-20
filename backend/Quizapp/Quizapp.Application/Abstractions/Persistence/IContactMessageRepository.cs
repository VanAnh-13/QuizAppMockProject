using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IContactMessageRepository
{
    void Add(ContactMessage message);
}
