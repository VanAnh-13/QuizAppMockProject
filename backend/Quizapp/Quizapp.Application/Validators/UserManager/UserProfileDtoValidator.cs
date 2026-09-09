using FluentValidation;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.UserManager;

public sealed class UserProfileDtoValidator : AbstractValidator<UserProfileDto>
{
    public UserProfileDtoValidator()
    {
        RuleFor(dto => dto.FullName)
            .NotEmpty()
            .MaximumLength(FieldLimits.FullNameLength);

        RuleFor(dto => dto.PhoneNumber)
            .MaximumLength(FieldLimits.PhoneNumberLength);

        RuleFor(dto => dto.Avatar)
            .MaximumLength(FieldLimits.ImageUrlLength);

        RuleFor(dto => dto.DateOfBirth)
            .Must(date => date is null || date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
    }
}
