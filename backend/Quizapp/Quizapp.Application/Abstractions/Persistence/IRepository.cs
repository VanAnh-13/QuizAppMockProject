using Quizapp.Application.DTOs.Common;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResultDto<T>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken);

    void Add(T entity);

    void Remove(T entity);
}
