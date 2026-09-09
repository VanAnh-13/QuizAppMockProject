using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.Services.UserManager;

public interface IUserService
{
    Task<UserDto> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PagedResultDto<UserDto>> GetListAsync(
        int pageNumber,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(
        CreateUserDto request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid userId,
        UpdateUserDto request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task UpdateRolesAsync(
        Guid userId,
        UpdateUserRolesDto request,
        CancellationToken cancellationToken = default);
}