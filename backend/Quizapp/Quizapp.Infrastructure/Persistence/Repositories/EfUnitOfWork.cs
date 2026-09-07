using Quizapp.Application.Abstractions.Persistence;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfUnitOfWork(QuizAppDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
