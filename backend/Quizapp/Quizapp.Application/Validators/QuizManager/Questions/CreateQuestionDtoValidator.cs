using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.QuizManager.Questions;

public sealed class CreateQuestionDtoValidator : AbstractValidator<CreateQuestionDto>
{
    public CreateQuestionDtoValidator()
    {
        RuleFor(dto => dto.Content).NotEmpty().MaximumLength(FieldLimits.ContentLength);
        RuleFor(dto => dto.Image).MaximumLength(FieldLimits.ImageUrlLength);
        RuleFor(dto => dto.QuestionType).IsInEnum();
        RuleFor(dto => dto.Level).IsInEnum();
        RuleFor(dto => dto.Answers).NotNull();
        RuleForEach(dto => dto.Answers).NotNull().SetValidator(new CreateQuestionAnswerDtoValidator());
    }
}
