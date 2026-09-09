using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfQuestionRepository(QuizAppDbContext db) : IQuestionRepository
{
    public Task<Question?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Questions.Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public async Task<PagedResultDto<Question>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Questions.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(q => q.Content.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(q => q.Content)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<Question>
            { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public void Add(Question entity) => db.Questions.Add(entity);

    public void Remove(Question entity) => db.Questions.Remove(entity);
}
