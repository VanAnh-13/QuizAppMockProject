using FluentValidation;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Domain.Constants;
using Quizapp.Domain.Entities;

namespace Quizapp.Application.Validators.QuizManager.Quizzes;

public sealed class CreateQuizDtoValidator : AbstractValidator<CreateQuizDto>
{
    public CreateQuizDtoValidator()
    {
        RuleFor(dto => dto.Title)
            .NotEmpty()
            .MaximumLength(FieldLimits.QuizTitleLength);

        RuleFor(dto => dto.Description)
            .MaximumLength(FieldLimits.QuizDescriptionLength);

        RuleFor(dto => dto.Duration)
            .GreaterThan(0);

        RuleFor(dto => dto.Image)
            .MaximumLength(FieldLimits.ImageUrlLength);

        RuleFor(dto => dto.PassedScore)
            .Must(double.IsFinite)
            .WithMessage("Passed score must be a finite number.")
            .GreaterThanOrEqualTo(Quiz.MinimumPassedScore)
            .LessThanOrEqualTo(Quiz.MaximumPassedScore);
    }
}
