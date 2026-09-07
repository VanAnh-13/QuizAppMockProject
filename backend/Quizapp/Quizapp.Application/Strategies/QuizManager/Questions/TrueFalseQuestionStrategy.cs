using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

public sealed class TrueFalseQuestionStrategy : IQuestionCreationStrategy
{
    public IReadOnlyCollection<QuestionType> SupportedTypes { get; } = Array.AsReadOnly([QuestionType.TrueFalse]);

    public void ValidateAnswers(IReadOnlyCollection<CreateQuestionAnswerDto> activeAnswers)
    {
        ArgumentNullException.ThrowIfNull(activeAnswers);

        QuestionAnswerRules.Require(activeAnswers.Count == QuestionAnswerRules.TrueFalseChoiceCount,
            $"True/false questions require exactly {QuestionAnswerRules.TrueFalseChoiceCount} active options.");

        QuestionAnswerRules.Require(
            activeAnswers.Count(answer => answer.IsCorrect) == QuestionAnswerRules.SingleCorrectAnswerCount,
            "True/false questions require exactly one active correct option.");
    }
}
