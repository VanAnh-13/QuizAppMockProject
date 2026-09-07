using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

public sealed class LongAnswerQuestionStrategy : IQuestionCreationStrategy
{
    public IReadOnlyCollection<QuestionType> SupportedTypes { get; } = Array.AsReadOnly([QuestionType.LongAnswer]);

    public void ValidateAnswers(IReadOnlyCollection<CreateQuestionAnswerDto> activeAnswers)
    {
        ArgumentNullException.ThrowIfNull(activeAnswers);
        QuestionAnswerRules.Require(activeAnswers.All(answer => answer.IsCorrect),
            "Any active reference answers for long-answer questions must be marked as correct.");
    }
}
