using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.QuizQuestion;
using QuizQuestionEntity = Quizapp.Domain.Entities.QuizQuestion;

namespace Quizapp.Application.Validators.QuizManager.QuizQuestion;

public sealed class AddQuestionToQuizDtoValidator : AbstractValidator<AddQuestionToQuizDto>
{
    public AddQuestionToQuizDtoValidator()
    {
        RuleFor(dto => dto.QuizId)
            .NotEmpty();

        RuleFor(dto => dto.QuestionId)
            .NotEmpty();

        RuleFor(dto => dto.Order)
            .GreaterThanOrEqualTo(QuizQuestionEntity.FirstOrder);
    }
}
