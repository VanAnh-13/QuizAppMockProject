using FluentValidation;
using QuizApp.Application.DTOs.Quizzes;

namespace QuizApp.Application.Validators.Quizzes;

public class QuizCreateViewModelValidator : AbstractValidator<QuizCreateViewModel>
{
    public QuizCreateViewModelValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Duration).InclusiveBetween(1, 600)
            .WithMessage("Duration must be between 1 and 600 minutes.");
    }
}

public class QuizEditViewModelValidator : AbstractValidator<QuizEditViewModel>
{
    public QuizEditViewModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid quiz id.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Duration).InclusiveBetween(1, 600)
            .WithMessage("Duration must be between 1 and 600 minutes.");
    }
}
