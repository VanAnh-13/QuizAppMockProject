using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IRoleRepository : IRepository<Role>
{
    Task<bool> NameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<bool> HasUsersAsync(Guid roleId, CancellationToken cancellationToken);
}
