using FluentValidation;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.Authentication;

public sealed class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordDtoValidator()
    {
        RuleFor(dto => dto.Email)
            .NotEmpty()
            .MaximumLength(FieldLimits.EmailLength)
            .EmailAddress();
    }
}
