using FluentValidation;
using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.RoleManager;

public sealed class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
{
    public CreateRoleDtoValidator()
    {
        RuleFor(dto => dto.RoleName).NotEmpty().MaximumLength(FieldLimits.NameLength);
        RuleFor(dto => dto.Description).MaximumLength(FieldLimits.ContentLength);
    }
}
