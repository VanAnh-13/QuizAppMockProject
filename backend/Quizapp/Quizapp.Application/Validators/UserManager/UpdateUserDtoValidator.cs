using FluentValidation;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.UserManager;

public sealed class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(dto => dto.Username)
            .NotEmpty()
            .MaximumLength(FieldLimits.NameLength);

        RuleFor(dto => dto.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(FieldLimits.EmailLength);

        RuleFor(dto => dto.Profile)
            .NotNull()
            .SetValidator(new UserProfileDtoValidator());
    }
}
