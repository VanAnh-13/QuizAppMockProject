using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.RoleManager;

namespace Quizapp.Application.Services.RoleManager;

public interface IRoleService
{
    Task<RoleDto> GetByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<RoleDto>> GetListAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(
        CreateRoleDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid roleId,
        UpdateRoleDto request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}