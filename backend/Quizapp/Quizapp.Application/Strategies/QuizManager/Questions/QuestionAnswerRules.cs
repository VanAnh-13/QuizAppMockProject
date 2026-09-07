using FluentValidation;
using FluentValidation.Results;
using Quizapp.Application.DTOs.QuizManager.Questions;

namespace Quizapp.Application.Strategies.QuizManager.Questions;

internal static class QuestionAnswerRules
{
    internal const int MinimumChoiceCount = 2;
    internal const int TrueFalseChoiceCount = 2;
    internal const int SingleCorrectAnswerCount = 1;

    internal static void Require(bool condition, string message)
    {
        if (!condition)
            throw new ValidationException([new ValidationFailure(nameof(CreateQuestionDto.Answers), message)]);
    }
}
