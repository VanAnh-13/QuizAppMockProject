using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

public interface IQuestionCreationStrategy
{
    IReadOnlyCollection<QuestionType> SupportedTypes { get; }

    void ValidateAnswers(IReadOnlyCollection<CreateQuestionAnswerDto> activeAnswers);
}
