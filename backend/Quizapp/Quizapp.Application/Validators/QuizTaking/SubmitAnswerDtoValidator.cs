using FluentValidation;
using Quizapp.Application.DTOs.QuizTaking;

namespace Quizapp.Application.Validators.QuizTaking;

public sealed class SubmitAnswerDtoValidator : AbstractValidator<SubmitAnswerDto>
{
    public SubmitAnswerDtoValidator()
    {
        RuleFor(dto => dto.QuestionId)
            .NotEmpty();

        RuleFor(dto => dto.AnswerIds)
            .NotNull()
            .Must(ids => ids is null || ids.Distinct()
                .Count() == ids.Count)
            .WithMessage("The same answer cannot be selected twice.");

        RuleForEach(dto => dto.AnswerIds)
            .NotEmpty();

        RuleFor(dto => dto)
            .Must(dto => HasValidResponse(dto.AnswerIds, dto.ResponseText))
            .WithMessage("Provide selected answers or non-empty response text, but not both.");
    }

    private static bool HasValidResponse(List<Guid>? answerIds, string? responseText)
    {
        return answerIds is not null
               && ((answerIds.Count > 0 && responseText is null)
                   || (answerIds.Count == 0 && !string.IsNullOrWhiteSpace(responseText)));
    }
}
