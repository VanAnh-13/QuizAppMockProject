using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.QuizManager.Questions;

public sealed class UpdateQuestionDtoValidator : AbstractValidator<UpdateQuestionDto>
{
    public UpdateQuestionDtoValidator()
    {
        RuleFor(dto => dto.Content)
            .NotEmpty()
            .MaximumLength(FieldLimits.ContentLength);

        RuleFor(dto => dto.Image)
            .MaximumLength(FieldLimits.ImageUrlLength);

        RuleFor(dto => dto.QuestionType)
            .IsInEnum();

        RuleFor(dto => dto.Level)
            .IsInEnum();
    }
}
