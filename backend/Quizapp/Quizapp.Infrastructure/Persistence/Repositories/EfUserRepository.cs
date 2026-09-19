using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfUserRepository(QuizAppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower(), cancellationToken);

    public async Task<PagedResultDto<User>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query.Include(u => u.Roles)
            .OrderBy(u => u.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<User>
            { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public void Add(User entity) => db.Users.Add(entity);

    public void Remove(User entity) => db.Users.Remove(entity);

    public Task<bool> UsernameExistsAsync(string username, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() && u.Id != excludedId, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower() && u.Id != excludedId, cancellationToken);
}
