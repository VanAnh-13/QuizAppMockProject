using FluentValidation;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Constants;
using Quizapp.Application.Validators.UserManager;

namespace Quizapp.Application.Validators.Authentication;

public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(dto => dto.Username)
            .NotEmpty()
            .MaximumLength(FieldLimits.NameLength);

        RuleFor(dto => dto.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(FieldLimits.EmailLength);

        RuleFor(dto => dto.Password)
            .NotEmpty()
            .MinimumLength(PasswordLimits.MinimumLength)
            .MaximumLength(PasswordLimits.MaximumLength);

        RuleFor(dto => dto.ConfirmPassword)
            .NotEmpty()
            .Equal(dto => dto.Password);

        RuleFor(dto => dto.Profile)
            .NotNull()
            .SetValidator(new UserProfileDtoValidator());
    }
}