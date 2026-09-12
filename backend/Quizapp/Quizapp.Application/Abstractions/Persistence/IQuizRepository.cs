using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IQuizRepository : IRepository<Quiz>
{
    Task<PagedResultDto<Quiz>> GetActiveListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken);
}
