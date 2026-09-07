using FluentValidation;
using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Factories.RoleManager;

public sealed class RoleFactory(IValidator<CreateRoleDto> validator) : IRoleFactory
{
    public Role Create(CreateRoleDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        validator.ValidateAndThrow(request);

        return new Role
        {
            Id = Guid.NewGuid(),
            RoleName = request.RoleName,
            Description = request.Description,
            IsActive = request.IsActive
        };
    }
}
