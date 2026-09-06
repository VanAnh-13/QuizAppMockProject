using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Answers;
using Quizapp.Domain.Constants;

namespace Quizapp.Application.Validators.QuizManager.Answers;

public sealed class UpdateAnswerDtoValidator : AbstractValidator<UpdateAnswerDto>
{
    public UpdateAnswerDtoValidator()
        => RuleFor(dto => dto.Text).NotEmpty().MaximumLength(FieldLimits.ContentLength);
}