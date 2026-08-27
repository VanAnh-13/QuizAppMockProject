using FluentValidation;
using QuizApp.Application.DTOs.Feedback;
using QuizApp.Application.DTOs.Roles;
using QuizApp.Application.DTOs.Users;

namespace QuizApp.Application.Validators.Users;

public class UserCreateViewModelValidator : AbstractValidator<UserCreateViewModel>
{
    public UserCreateViewModelValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }
}

public class UserEditViewModelValidator : AbstractValidator<UserEditViewModel>
{
    public UserEditViewModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid user id.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
    }
}

public class RoleCreateViewModelValidator : AbstractValidator<RoleCreateViewModel>
{
    public RoleCreateViewModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}

public class RoleEditViewModelValidator : AbstractValidator<RoleEditViewModel>
{
    public RoleEditViewModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid role id.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(512);
    }
}

public class FeedbackCreateViewModelValidator : AbstractValidator<FeedbackCreateViewModel>
{
    public FeedbackCreateViewModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Subject).MaximumLength(256);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
    }
}
