using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfUnitOfWork(QuizAppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
            when (exception.Entries is [{ Entity: QuizAttempt }])
        {
            var attempt = (QuizAttempt)exception.Entries[0].Entity;
            throw new ConflictException(nameof(QuizAttempt), nameof(QuizAttempt.Id), attempt.Id.ToString());
        }
    }
}
