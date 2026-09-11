using FluentValidation;
using Quizapp.Application.DTOs.QuizTaking;

namespace Quizapp.Application.Validators.QuizTaking;

public sealed class AttemptRevisionDtoValidator : AbstractValidator<AttemptRevisionDto>
{
    public AttemptRevisionDtoValidator()
    {
        RuleFor(dto => dto.Revision).NotNull().GreaterThanOrEqualTo(0);
    }
}
