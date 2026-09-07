using FluentValidation;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.Common;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.UserManager;

public sealed class UserService(
    IUserRepository users,
    IRoleRepository roles,
    IUnitOfWork unitOfWork,
    UserProvisioning provisioning,
    ServiceAuthorization authorization,
    TimeProvider clock,
    IValidator<CreateUserDto> createValidator,
    IValidator<UpdateUserDto> updateValidator,
    IValidator<UpdateUserRolesDto> rolesValidator) : IUserService
{
    public async Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        return DtoMapping.ToDto(await FindAsync(userId, cancellationToken));
    }

    public async Task<PagedResultDto<UserDto>> GetListAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ServiceRules.ValidatePage(pageNumber, pageSize);
        return ServiceRules.MapPage(await users.GetListAsync(pageNumber, pageSize, ServiceRules.Search(search), cancellationToken), DtoMapping.ToDto);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto request, CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await provisioning.CreateAsync(request.Username, request.Email, request.Password,
            request.Profile, request.IsActive, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return DtoMapping.ToDto(user);
    }

    public async Task UpdateAsync(Guid userId, UpdateUserDto request, CancellationToken cancellationToken = default)
    {
        var administrator = await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await FindAsync(userId, cancellationToken);
        PreventSelfDeactivation(administrator.Id, userId, request.IsActive);
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        await provisioning.EnsureUniqueAsync(username, email, userId, cancellationToken);
        user.Username = username;
        user.Email = email;
        user.FullName = request.Profile.FullName.Trim();
        user.PhoneNumber = request.Profile.PhoneNumber;
        user.DateOfBirth = request.Profile.DateOfBirth;
        user.Avatar = request.Profile.Avatar;
        user.IsActive = request.IsActive;
        Touch(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var administrator = await authorization.RequireAdminAsync(cancellationToken);
        PreventSelfDeactivation(administrator.Id, userId, isActive);
        var user = await FindAsync(userId, cancellationToken);
        user.IsActive = isActive;
        Touch(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateRolesAsync(Guid userId, UpdateUserRolesDto request, CancellationToken cancellationToken = default)
    {
        var administrator = await authorization.RequireAdminAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(request);
        await rolesValidator.ValidateAndThrowAsync(request, cancellationToken);
        var user = await FindAsync(userId, cancellationToken);
        var assignments = await roles.GetByIdsAsync(request.RoleIds, cancellationToken);
        var missingId = request.RoleIds.Except(assignments.Select(role => role.Id)).FirstOrDefault();
        if (missingId != Guid.Empty)
            throw new NotFoundException(nameof(Role), missingId);
        if (assignments.Any(role => !role.IsActive))
            throw new BusinessRuleException("InactiveRole", "Only active roles can be assigned.");
        if (administrator.Id == userId && !assignments.Any(role =>
                string.Equals(role.RoleName, ServiceAuthorization.AdministratorRole, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException("SelfRoleRemoval", "You cannot remove your own administrator role.");

        var selectedIds = assignments.Select(role => role.Id).ToHashSet();
        foreach (var role in user.Roles.Where(role => !selectedIds.Contains(role.Id)).ToArray())
            user.Roles.Remove(role);
        foreach (var role in assignments.Where(role => user.Roles.All(existing => existing.Id != role.Id)))
            user.Roles.Add(role);
        Touch(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(User), id);

    private void Touch(User user)
    {
        user.UpdateAt = clock.GetUtcNow().UtcDateTime;
        user.SecurityStamp = Guid.NewGuid();
    }

    private static void PreventSelfDeactivation(Guid administratorId, Guid targetId, bool isActive)
    {
        if (administratorId == targetId && !isActive)
            throw new BusinessRuleException("SelfDeactivation", "You cannot deactivate your own administrator account.");
    }
}
