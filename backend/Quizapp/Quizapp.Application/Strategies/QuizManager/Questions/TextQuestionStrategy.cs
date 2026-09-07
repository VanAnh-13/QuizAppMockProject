using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

public sealed class TextQuestionStrategy : IQuestionCreationStrategy
{
    public IReadOnlyCollection<QuestionType> SupportedTypes { get; } =
        Array.AsReadOnly([QuestionType.FillInTheBlanks, QuestionType.ShortAnswer]);

    public void ValidateAnswers(IReadOnlyCollection<CreateQuestionAnswerDto> activeAnswers)
    {
        ArgumentNullException.ThrowIfNull(activeAnswers);
        QuestionAnswerRules.Require(activeAnswers.Count > 0,
            "Text questions require at least one active accepted answer.");
        QuestionAnswerRules.Require(activeAnswers.All(answer => answer.IsCorrect),
            "Active text answers must be marked as correct accepted answers.");
    }
}
