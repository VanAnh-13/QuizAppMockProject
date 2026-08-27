using FluentValidation;
using QuizApp.Application.DTOs.Answers;
using QuizApp.Application.DTOs.Questions;

namespace QuizApp.Application.Validators.Questions;

public class QuestionCreateViewModelValidator : AbstractValidator<QuestionCreateViewModel>
{
    public QuestionCreateViewModelValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.QuestionType).IsInEnum();
    }
}

public class QuestionEditViewModelValidator : AbstractValidator<QuestionEditViewModel>
{
    public QuestionEditViewModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid question id.");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.QuestionType).IsInEnum();
    }
}

public class AnswerCreateViewModelValidator : AbstractValidator<AnswerCreateViewModel>
{
    public AnswerCreateViewModelValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.QuestionId).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid question id.");
    }
}

public class AnswerEditViewModelValidator : AbstractValidator<AnswerEditViewModel>
{
    public AnswerEditViewModelValidator()
    {
        RuleFor(x => x.Id).NotEmpty().Must(id => Guid.TryParse(id, out _)).WithMessage("Invalid answer id.");
        RuleFor(x => x.Content).NotEmpty().MaximumLength(1000);
    }
}
