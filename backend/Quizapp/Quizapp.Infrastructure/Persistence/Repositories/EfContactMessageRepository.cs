using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfContactMessageRepository(QuizAppDbContext db) : IContactMessageRepository
{
    public void Add(ContactMessage message) => db.ContactMessages.Add(message);
}
