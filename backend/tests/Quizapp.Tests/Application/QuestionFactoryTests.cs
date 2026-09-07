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
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();
        var activeCorrect = new CreateQuestionAnswerDto.Builder().WithText("Correct").WithIsCorrect(true).Build();
        var inactive = new CreateQuestionAnswerDto.Builder().WithText("Disabled").WithIsCorrect(true).WithIsActive(false).Build();
        var builder = new CreateQuestionDto.Builder().WithContent("Question").WithLevel(QuestionLevel.Easy)
            .WithQuestionType(QuestionType.SingleChoice).WithAnswers([activeCorrect, inactive]);

        Assert.Throws<ValidationException>(() => factory.Create(builder.Build()));

        var activeIncorrect = new CreateQuestionAnswerDto.Builder().WithText("Incorrect").WithIsCorrect(false).Build();
        var question = factory.Create(builder.WithAnswers([activeCorrect, activeIncorrect, inactive]).Build());
        Assert.Equal(3, question.Answers.Count);
        Assert.Equal("Disabled", Assert.Single(question.Answers, answer => !answer.IsActive).Text);
    }

    [Fact]
    public void Invalid_metadata_and_nested_answers_fail_validation_before_creation()
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
        var builder = new CreateQuestionDto.Builder().WithContent("Question").WithLevel(QuestionLevel.Easy)
            .WithQuestionType((QuestionType)0);
        var invalidType = Assert.Throws<ValidationException>(() => factory.Create(builder.Build()));
        Assert.Contains(invalidType.Errors, error => error.PropertyName == nameof(CreateQuestionDto.QuestionType));

        builder.WithQuestionType(QuestionType.LongAnswer);
        var invalidAnswer = Assert.Throws<ValidationException>(() => factory.Create(builder.WithAnswers([
            new CreateQuestionAnswerDto.Builder().WithText(" ").WithIsCorrect(true).Build()
        ]).Build()));
        Assert.Contains(invalidAnswer.Errors, error => error.PropertyName == "Answers[0].Text");
        Assert.Throws<ValidationException>(() => factory.Create(builder.WithAnswers([null!]).Build()));
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
        var request = new CreateQuestionDto.Builder().WithContent("Question").WithLevel(QuestionLevel.Easy)
            .WithQuestionType(QuestionType.LongAnswer).Build();

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
    public void Question_types_apply_their_own_answer_rules(QuestionType type, int answerCount, int correctCount, bool valid)
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();
        var answers = Enumerable.Range(0, answerCount).Select(index =>
            new CreateQuestionAnswerDto.Builder().WithText($"Option {index}").WithIsCorrect(index < correctCount).Build());
        var request = new CreateQuestionDto.Builder().WithContent("Question")
            .WithLevel(QuestionLevel.Easy).WithQuestionType(type).WithAnswers(answers).Build();

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
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IQuestionFactory>();
        var request = new CreateQuestionDto.Builder().WithContent("Which option is correct?")
            .WithLevel(QuestionLevel.Medium).WithQuestionType(QuestionType.SingleChoice)
            .WithIsActive(true).WithImage("question.png")
            .WithAnswers([
                new CreateQuestionAnswerDto.Builder().WithText("Correct option").WithIsCorrect(true).Build(),
                new CreateQuestionAnswerDto.Builder().WithText("Incorrect option").WithIsCorrect(false).Build()
            ]).Build();

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
        Assert.Equal(2, question.Answers.Select(answer => answer.Id).Distinct().Count());
    }
}
