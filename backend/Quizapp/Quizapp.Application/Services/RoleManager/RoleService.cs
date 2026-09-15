using FluentValidation;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.RoleManager;

public sealed class RoleService(
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    IValidator<CreateRoleDto> createValidator,
    IValidator<UpdateRoleDto> updateValidator,
    ServiceAuthorization authorization) : IRoleService
{
    public async Task<RoleDto> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);

        return DtoMapping.ToDto(await FindAsync(roleId, cancellationToken));
    }

    public async Task<PagedResultDto<RoleDto>> GetListAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ServiceRules.ValidatePage(pageNumber, pageSize);

        return ServiceRules.MapPage(
            await roles.GetListAsync(pageNumber, pageSize, ServiceRules.Search(search), cancellationToken),
            DtoMapping.ToDto);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleDto request, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);

        ArgumentNullException.ThrowIfNull(request);
        createValidator.ValidateAndThrow(request);

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = request.RoleName.Trim(),
            Description = request.Description,
            IsActive = request.IsActive
        };

        await EnsureUniqueAsync(role.RoleName, null, cancellationToken);

        roles.Add(role);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return DtoMapping.ToDto(role);
    }

    public async Task UpdateAsync(Guid roleId, UpdateRoleDto request, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);

        ArgumentNullException.ThrowIfNull(request);

        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var role = await FindAsync(roleId, cancellationToken);
        var name = request.RoleName.Trim();

        await EnsureUniqueAsync(name, roleId, cancellationToken);

        if (string.Equals(role.RoleName, ServiceAuthorization.AdministratorRole, StringComparison.OrdinalIgnoreCase)
            && (!request.IsActive || !string.Equals(name, ServiceAuthorization.AdministratorRole,
                StringComparison.OrdinalIgnoreCase))
            && await roles.HasUsersAsync(roleId, cancellationToken))
            throw new BusinessRuleException("AdministratorRoleInUse",
                "An assigned administrator role cannot be renamed or disabled.");

        role.RoleName = name;
        role.Description = request.Description;
        role.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);

        var role = await FindAsync(roleId, cancellationToken);

        if (await roles.HasUsersAsync(roleId, cancellationToken))
            throw new BusinessRuleException("RoleInUse", "Remove user assignments before deleting this role.");

        roles.Remove(role);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await roles.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Role), id);

    private async Task EnsureUniqueAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await roles.NameExistsAsync(name, excludedId, cancellationToken))
            throw new ConflictException(nameof(Role), nameof(Role.RoleName), name);
    }
}
