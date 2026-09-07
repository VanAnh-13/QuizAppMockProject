using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

public sealed class MultipleChoiceQuestionStrategy : IQuestionCreationStrategy
{
    public IReadOnlyCollection<QuestionType> SupportedTypes { get; } = Array.AsReadOnly([QuestionType.MultipleChoice]);

    public void ValidateAnswers(IReadOnlyCollection<CreateQuestionAnswerDto> activeAnswers)
    {
        ArgumentNullException.ThrowIfNull(activeAnswers);

        QuestionAnswerRules.Require(activeAnswers.Count >= QuestionAnswerRules.MinimumChoiceCount,
            $"Multiple-choice questions require at least {QuestionAnswerRules.MinimumChoiceCount} active options.");

        QuestionAnswerRules.Require(activeAnswers.Any(answer => answer.IsCorrect),
            "Multiple-choice questions require at least one active correct option.");
    }
}
