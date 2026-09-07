using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Answers;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.QuizManager.Answers;

public sealed class CreateAnswerDtoValidator : AbstractValidator<CreateAnswerDto>
{
    public CreateAnswerDtoValidator()
    {
        RuleFor(dto => dto.Text)
            .NotEmpty()
            .MaximumLength(FieldLimits.ContentLength);

        RuleFor(dto => dto.QuestionId)
            .NotEmpty();
    }
}