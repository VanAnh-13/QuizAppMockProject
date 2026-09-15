using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application;
using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Factories.QuizManager.Questions;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Application.Validators.QuizManager.Questions;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Application;

public class QuestionFactoryTests
{
    [Fact]
    public void Inactive_options_are_preserved_but_do_not_satisfy_active_answer_rules()
    {
        using var provider = new ServiceCollection().AddApplication()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();

        var activeCorrect = new CreateQuestionAnswerDto { Text = "Correct", IsCorrect = true };
        var inactive = new CreateQuestionAnswerDto { Text = "Disabled", IsCorrect = true, IsActive = false };
        var request = new CreateQuestionDto
        {
            Content = "Question",
            Level = QuestionLevel.Easy,
            QuestionType = QuestionType.SingleChoice,
            Answers = [activeCorrect, inactive]
        };

        Assert.Throws<ValidationException>(() => factory.Create(request));

        var activeIncorrect = new CreateQuestionAnswerDto { Text = "Incorrect", IsCorrect = false };
        request.Answers = [activeCorrect, activeIncorrect, inactive];
        var question = factory.Create(request);

        Assert.Equal(3, question.Answers.Count);

        Assert.Equal("Disabled", Assert.Single(question.Answers, answer => !answer.IsActive)
            .Text);
    }

    [Fact]
    public void Invalid_metadata_and_nested_answers_fail_validation_before_creation()
    {
        using var provider = new ServiceCollection().AddApplication()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));

        var request = new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = (QuestionType)0
        };

        var invalidType = Assert.Throws<ValidationException>(() => factory.Create(request));
        Assert.Contains(invalidType.Errors, error => error.PropertyName == nameof(CreateQuestionDto.QuestionType));

        request.QuestionType = QuestionType.LongAnswer;
        request.Answers = [new CreateQuestionAnswerDto { Text = " ", IsCorrect = true }];

        var invalidAnswer = Assert.Throws<ValidationException>(() => factory.Create(request));

        Assert.Contains(invalidAnswer.Errors, error => error.PropertyName == "Answers[0].Text");

        request.Answers = [null!];
        Assert.Throws<ValidationException>(() => factory.Create(request));

        Assert.Throws<ValidationException>(() => factory.Create(new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = QuestionType.LongAnswer, Answers = null!
        }));
    }

    [Fact]
    public void Missing_or_conflicting_strategies_are_reported_instead_of_using_a_fallback()
    {
        var validator = new CreateQuestionDtoValidator();
        var factory = new QuestionFactory(validator, []);

        var request = new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = QuestionType.LongAnswer
        };

        Assert.Throws<NotSupportedException>(() => factory.Create(request));

        Assert.Throws<InvalidOperationException>(() => new QuestionFactory(validator,
            [new SingleChoiceQuestionStrategy(), new SingleChoiceQuestionStrategy()]));
    }

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
    public void Question_types_apply_their_own_answer_rules(QuestionType type, int answerCount, int correctCount,
        bool valid)
    {
        using var provider = new ServiceCollection().AddApplication()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();

        var request = new CreateQuestionDto
        {
            Content = "Question",
            Level = QuestionLevel.Easy,
            QuestionType = type,
            Answers =
            [
                .. Enumerable.Range(0, answerCount).Select(index => new CreateQuestionAnswerDto
                {
                    Text = $"Option {index}", IsCorrect = index < correctCount
                })
            ]
        };

        if (valid)
        {
            var question = factory.Create(request);
            Assert.Equal(type, question.QuestionType);
            Assert.Equal(answerCount, question.Answers.Count);
        }
        else
        {
            var exception = Assert.Throws<ValidationException>(() => factory.Create(request));
            Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateQuestionDto.Answers));
        }
    }

    [Fact]
    public void Factory_creates_a_complete_question_graph_with_server_generated_ids()
    {
        using var provider = new ServiceCollection().AddApplication()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();

        var request = new CreateQuestionDto
        {
            Content = "Which option is correct?",
            Level = QuestionLevel.Medium,
            QuestionType = QuestionType.SingleChoice,
            IsActive = true,
            Image = "question.png",
            Answers =
            [
                new CreateQuestionAnswerDto { Text = "Correct option", IsCorrect = true },
                new CreateQuestionAnswerDto { Text = "Incorrect option", IsCorrect = false }
            ]
        };

        var question = factory.Create(request);
        var another = factory.Create(request);

        Assert.NotEqual(Guid.Empty, question.Id);
        Assert.NotEqual(question.Id, another.Id);
        Assert.Equal("Which option is correct?", question.Content);
        Assert.Equal("question.png", question.Image);
        Assert.Equal(QuestionLevel.Medium, question.Level);
        Assert.Equal(QuestionType.SingleChoice, question.QuestionType);
        Assert.True(question.IsActive);
        Assert.Equal(2, question.Answers.Count);
        Assert.Single(question.Answers, answer => answer.IsCorrect);

        Assert.All(question.Answers, answer =>
        {
            Assert.NotEqual(Guid.Empty, answer.Id);
            Assert.Equal(question.Id, answer.QuestionId);
            Assert.Same(question, answer.QuestionNavigation);
            Assert.True(answer.IsActive);
            Assert.DoesNotContain(another.Answers, other => other.Id == answer.Id);
        });

        Assert.Equal(2, question.Answers.Select(answer => answer.Id)
            .Distinct()
            .Count());
    }
}
