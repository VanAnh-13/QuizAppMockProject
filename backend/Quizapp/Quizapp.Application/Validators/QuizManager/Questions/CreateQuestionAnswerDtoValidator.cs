using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.QuizManager.Questions;

public sealed class CreateQuestionAnswerDtoValidator : AbstractValidator<CreateQuestionAnswerDto>
{
    public CreateQuestionAnswerDtoValidator()
    {
        RuleFor(dto => dto.Text)
            .NotEmpty()
            .MaximumLength(FieldLimits.ContentLength);
    }
}
