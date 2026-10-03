using FluentValidation;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.Authentication;

public sealed class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(dto => dto.UserId)
            .NotEmpty();

        RuleFor(dto => dto.Token)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(dto => dto.NewPassword)
            .NotEmpty()
            .MinimumLength(PasswordLimits.MinimumLength)
            .MaximumLength(PasswordLimits.MaximumLength);

        RuleFor(dto => dto.ConfirmNewPassword)
            .NotEmpty()
            .Equal(dto => dto.NewPassword);
    }
}
