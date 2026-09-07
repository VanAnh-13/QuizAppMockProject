using Microsoft.EntityFrameworkCore;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Entities;

namespace Quizapp.Infrastructure.Persistence.Repositories;

public sealed class EfRoleRepository(QuizAppDbContext db) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<PagedResultDto<Role>> GetListAsync(int pageNumber, int pageSize, string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Roles.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(r => r.RoleName.Contains(search));

        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderBy(r => r.RoleName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<Role>
            { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public void Add(Role entity) => db.Roles.Add(entity);

    public void Remove(Role entity) => db.Roles.Remove(entity);

    public Task<bool> NameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken) =>
        db.Roles.AnyAsync(r => r.RoleName.ToLower() == name.ToLower() && r.Id != excludedId, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await db.Roles.Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

    public Task<bool> HasUsersAsync(Guid roleId, CancellationToken cancellationToken) =>
        db.UserRoles.AnyAsync(ur => ur.RoleId == roleId, cancellationToken);
}
