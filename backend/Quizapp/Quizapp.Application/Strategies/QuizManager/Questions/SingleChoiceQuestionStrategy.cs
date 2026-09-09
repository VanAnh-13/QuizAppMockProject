using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

public sealed class SingleChoiceQuestionStrategy : IQuestionCreationStrategy
{
    public IReadOnlyCollection<QuestionType> SupportedTypes { get; } = Array.AsReadOnly([QuestionType.SingleChoice]);

    public void ValidateAnswers(IReadOnlyCollection<CreateQuestionAnswerDto> activeAnswers)
    {
        ArgumentNullException.ThrowIfNull(activeAnswers);

        QuestionAnswerRules.Require(activeAnswers.Count >= QuestionAnswerRules.MinimumChoiceCount,
            $"Single-choice questions require at least {QuestionAnswerRules.MinimumChoiceCount} active options.");

        QuestionAnswerRules.Require(
            activeAnswers.Count(answer => answer.IsCorrect) == QuestionAnswerRules.SingleCorrectAnswerCount,
            "Single-choice questions require exactly one active correct option.");
    }
}
