using FluentValidation;
using QuizApp.Application.DTOs.Auth;

namespace QuizApp.Application.Validators.Auth;

public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
{
    public RegisterViewModelValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).NotNull()
            .Must(dob => dob is null || dob.Value <= DateTime.UtcNow.AddYears(-5))
            .WithMessage("You must be at least 5 years old.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }
}

public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
{
    public LoginViewModelValidator()
    {
        RuleFor(x => x.UserName).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class ChangePasswordViewModelValidator : AbstractValidator<ChangePasswordViewModel>
{
    public ChangePasswordViewModelValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmNewPassword).NotEmpty().Equal(x => x.NewPassword)
            .WithMessage("Passwords do not match.");
    }
}

public class UpdateProfileViewModelValidator : AbstractValidator<UpdateProfileViewModel>
{
    public UpdateProfileViewModelValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DateOfBirth).NotNull()
            .Must(dob => dob is null || dob.Value <= DateTime.UtcNow)
            .WithMessage("Date of birth must be in the past.");
    }
}
