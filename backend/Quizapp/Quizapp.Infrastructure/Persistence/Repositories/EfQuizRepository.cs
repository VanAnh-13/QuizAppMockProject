using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfQuizRepository(QuizAppDbContext db) : IQuizRepository
{
    public Task<Quiz?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Quizzes
            .Include(q => q.QuizQuestions)
            .ThenInclude(qq => qq.QuestionNavigation)
            .ThenInclude(question => question.Answers)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<PagedResultDto<Quiz>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Quizzes.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(q => q.Title.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(q => q.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<Quiz>
            { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<PagedResultDto<Quiz>> GetActiveListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Quizzes.Where(q => q.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(q => q.Title.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(q => q.Title)
            .Include(q => q.QuizQuestions)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<Quiz>
            { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public void Add(Quiz entity) => db.Quizzes.Add(entity);

    public void Remove(Quiz entity) => db.Quizzes.Remove(entity);
}
