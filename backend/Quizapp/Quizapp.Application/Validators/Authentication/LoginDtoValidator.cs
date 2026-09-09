using FluentValidation;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.Authentication;

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(dto => dto.Username)
            .NotEmpty()
            .MaximumLength(FieldLimits.NameLength);

        RuleFor(dto => dto.Password)
            .NotEmpty();
    }
}