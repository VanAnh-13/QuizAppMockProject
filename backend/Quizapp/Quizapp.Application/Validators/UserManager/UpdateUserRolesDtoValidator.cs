using FluentValidation;
using Quizapp.Application.DTOs.UserManager;

namespace Quizapp.Application.Validators.UserManager;

public sealed class UpdateUserRolesDtoValidator : AbstractValidator<UpdateUserRolesDto>
{
    public UpdateUserRolesDtoValidator()
    {
        RuleFor(dto => dto.RoleIds)
            .NotNull()
            .Must(ids => ids is null || ids.Distinct()
                .Count() == ids.Count)
            .WithMessage("A role can only be assigned once.");

        RuleForEach(dto => dto.RoleIds)
            .NotEmpty();
    }
}
