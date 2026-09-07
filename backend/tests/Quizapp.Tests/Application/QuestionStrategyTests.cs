using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Application;

public class QuestionStrategyTests
{
    [Theory]
    [InlineData(QuestionType.SingleChoice, 2, 1, true)]
    [InlineData(QuestionType.SingleChoice, 2, 2, false)]
    [InlineData(QuestionType.SingleChoice, 1, 1, false)]
    [InlineData(QuestionType.MultipleChoice, 3, 2, true)]
    [InlineData(QuestionType.MultipleChoice, 2, 1, true)]
    [InlineData(QuestionType.MultipleChoice, 2, 0, false)]
    [InlineData(QuestionType.TrueFalse, 2, 1, true)]
    [InlineData(QuestionType.TrueFalse, 3, 1, false)]
    [InlineData(QuestionType.TrueFalse, 2, 2, false)]
    [InlineData(QuestionType.FillInTheBlanks, 2, 2, true)]
    [InlineData(QuestionType.FillInTheBlanks, 0, 0, false)]
    [InlineData(QuestionType.FillInTheBlanks, 2, 1, false)]
    [InlineData(QuestionType.ShortAnswer, 1, 1, true)]
    [InlineData(QuestionType.ShortAnswer, 0, 0, false)]
    [InlineData(QuestionType.LongAnswer, 0, 0, true)]
    [InlineData(QuestionType.LongAnswer, 1, 1, true)]
    [InlineData(QuestionType.LongAnswer, 1, 0, false)]
    public void Registered_strategy_enforces_answer_rules_without_a_factory(
        QuestionType type, int answerCount, int correctCount, bool valid)
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var strategy = Assert.Single(scope.ServiceProvider.GetServices<IQuestionCreationStrategy>(),
            candidate => candidate.SupportedTypes.Contains(type));
        var answers = Enumerable.Range(0, answerCount).Select(index =>
            new CreateQuestionAnswerDto
            {
                Text = $"Option {index}",
                IsCorrect = index < correctCount
            }).ToArray();

        if (valid)
        {
            strategy.ValidateAnswers(answers);
        }
        else
        {
            var exception = Assert.Throws<ValidationException>(() => strategy.ValidateAnswers(answers));
            Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateQuestionDto.Answers));
        }
    }
}
