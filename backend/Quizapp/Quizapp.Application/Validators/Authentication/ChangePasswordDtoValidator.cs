using FluentValidation;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.Authentication;

public sealed class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordDtoValidator()
    {
        RuleFor(dto => dto.CurrentPassword).NotEmpty().MaximumLength(PasswordLimits.MaximumLength);
        RuleFor(dto => dto.NewPassword).NotEmpty().MinimumLength(PasswordLimits.MinimumLength)
            .MaximumLength(PasswordLimits.MaximumLength).NotEqual(dto => dto.CurrentPassword);
        RuleFor(dto => dto.ConfirmNewPassword).NotEmpty().Equal(dto => dto.NewPassword);
    }
}
