using Quizapp.Application.DTOs.QuizManager.Questions;
using Quizapp.Application.Services.QuestionManager;
using Quizapp.Domain.Enums;

namespace Quizapp.Tests.Application.Services;

public class QuestionCreationTests
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
    public async Task Create_enforces_the_answer_shape_for_each_question_type(
        QuestionType type, int answerCount, int correctCount, bool valid)
    {
        using var context = new ServiceTestContext();
        var questions = context.Get<IQuestionService>();
        var request = Request(type, answerCount, correctCount);

        if (!valid)
        {
            var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => questions.CreateAsync(request));
            Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateQuestionDto.Answers));
            return;
        }

        var created = await questions.CreateAsync(request);
        var stored = context.Questions.Rows.Single(question => question.Id == created.Id);
        Assert.Equal(type, stored.QuestionType);
        Assert.Equal(answerCount, stored.Answers.Count);
    }

    [Fact]
    public async Task Create_keeps_inactive_answers_but_does_not_count_them_toward_the_shape()
    {
        using var context = new ServiceTestContext();
        var questions = context.Get<IQuestionService>();
        var activeCorrect = new CreateQuestionAnswerDto { Text = "Correct", IsCorrect = true };
        var inactive = new CreateQuestionAnswerDto { Text = "Disabled", IsCorrect = true, IsActive = false };

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => questions.CreateAsync(new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = QuestionType.SingleChoice,
            Answers = [activeCorrect, inactive]
        }));

        var created = await questions.CreateAsync(new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = QuestionType.SingleChoice,
            Answers = [activeCorrect, new CreateQuestionAnswerDto { Text = "Incorrect" }, inactive]
        });

        var stored = context.Questions.Rows.Single(question => question.Id == created.Id);
        Assert.Equal("Disabled", Assert.Single(stored.Answers, answer => !answer.IsActive).Text);
    }

    [Fact]
    public async Task Create_rejects_invalid_question_data_before_saving()
    {
        using var context = new ServiceTestContext();
        var questions = context.Get<IQuestionService>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => questions.CreateAsync(null!));
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => questions.CreateAsync(new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = (QuestionType)0
        }));
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => questions.CreateAsync(new CreateQuestionDto
        {
            Content = "Question", Level = QuestionLevel.Easy, QuestionType = QuestionType.LongAnswer,
            Answers = [new CreateQuestionAnswerDto { Text = " ", IsCorrect = true }]
        }));
        Assert.Empty(context.Questions.Rows);
    }

    [Fact]
    public async Task Create_builds_a_question_graph_with_server_generated_ids()
    {
        using var context = new ServiceTestContext();
        var questions = context.Get<IQuestionService>();
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
                new CreateQuestionAnswerDto { Text = "Incorrect option" }
            ]
        };

        var first = await questions.CreateAsync(request);
        var second = await questions.CreateAsync(request);
        var stored = context.Questions.Rows.Single(question => question.Id == first.Id);

        Assert.NotEqual(Guid.Empty, stored.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Which option is correct?", stored.Content);
        Assert.Equal("question.png", stored.Image);
        Assert.Equal(QuestionLevel.Medium, stored.Level);
        Assert.True(stored.IsActive);
        Assert.All(stored.Answers, answer =>
        {
            Assert.NotEqual(Guid.Empty, answer.Id);
            Assert.Equal(stored.Id, answer.QuestionId);
            Assert.Same(stored, answer.QuestionNavigation);
        });
    }

    private static CreateQuestionDto Request(QuestionType type, int answerCount, int correctCount) => new()
    {
        Content = "Question",
        Level = QuestionLevel.Easy,
        QuestionType = type,
        Answers =
        [
            ..Enumerable.Range(0, answerCount).Select(index => new CreateQuestionAnswerDto
            {
                Text = $"Option {index}", IsCorrect = index < correctCount
            })
        ]
    };
}
