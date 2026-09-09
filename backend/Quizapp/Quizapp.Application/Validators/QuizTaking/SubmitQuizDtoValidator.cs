using FluentValidation;
using Quizapp.Application.DTOs.QuizTaking;

namespace Quizapp.Application.Validators.QuizTaking;

public sealed class SubmitQuizDtoValidator : AbstractValidator<SubmitQuizDto>
{
    public SubmitQuizDtoValidator()
    {
        RuleFor(dto => dto.AttemptId).NotEmpty();
        RuleFor(dto => dto.Revision).GreaterThanOrEqualTo(0);

        RuleFor(dto => dto.Answers)
            .NotNull()
            .Must(HaveDistinctQuestions)
            .WithMessage("Submit each question only once.");

        RuleForEach(dto => dto.Answers)
            .NotNull()
            .SetValidator(new SubmitAnswerDtoValidator());
    }

    private static bool HaveDistinctQuestions(IReadOnlyCollection<SubmitAnswerDto?>? answers)
    {
        return answers is null
               || answers.Select(answer => answer?.QuestionId)
                   .Distinct()
                   .Count() == answers.Count;
    }
}
