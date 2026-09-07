using Quizapp.Domain.Entities;

namespace Quizapp.Application.Abstractions.Persistence;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(string username, Guid? excludedId, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, Guid? excludedId, CancellationToken cancellationToken);
}
