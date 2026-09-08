using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfQuizAttemptRepository(QuizAppDbContext db) : IQuizAttemptRepository
{
    public Task<QuizAttempt?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.QuizAttempts
            .Include(a => a.UserAnswers)
            .Include(a => a.QuizNavigation)
            .ThenInclude(q => q.QuizQuestions)
            .ThenInclude(qq => qq.QuestionNavigation)
            .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<PagedResultDto<QuizAttempt>> GetHistoryAsync(Guid userId, int pageNumber, int pageSize,
        Guid? quizId, CancellationToken cancellationToken)
    {
        var query = db.QuizAttempts
            .Include(a => a.QuizNavigation)
            .Where(a => a.UserId == userId && a.SubmitAt != null);

        if (quizId.HasValue)
            query = query.Where(a => a.QuizId == quizId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderByDescending(a => a.SubmitAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<QuizAttempt>
        {
            Items = items,
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public void Add(QuizAttempt attempt) => db.QuizAttempts.Add(attempt);
}
