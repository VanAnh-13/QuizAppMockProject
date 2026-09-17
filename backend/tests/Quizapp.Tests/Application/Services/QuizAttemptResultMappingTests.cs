using System.Text.Json;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Application.Services.QuizTaking;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Application.Services;

public class QuizAttemptResultMappingTests
{
    [Theory]
    [InlineData(QuestionType.SingleChoice, true)]
    [InlineData(QuestionType.MultipleChoice, true)]
    [InlineData(QuestionType.TrueFalse, true)]
    [InlineData(QuestionType.FillInTheBlanks, true)]
    [InlineData(QuestionType.ShortAnswer, true)]
    [InlineData(QuestionType.LongAnswer, true)]
    [InlineData(QuestionType.SingleChoice, false)]
    public async Task Submission_and_reloaded_result_share_the_original_question_and_response(
        QuestionType type, bool answered)
    {
        using var context = new ServiceTestContext();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var question = TestEntities.Question(type);
        question.IsActive = true;
        question.Image = "original.png";
        question.Level = QuestionLevel.Medium;
        var firstAnswer = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, QuestionNavigation = question,
            Text = "First answer", IsCorrect = true, IsActive = true
        };
        var secondAnswer = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, QuestionNavigation = question,
            Text = "Second answer", IsCorrect = true, IsActive = true
        };
        question.Answers.Add(firstAnswer);
        question.Answers.Add(secondAnswer);
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        context.Quizzes.Add(quiz);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);
        question.Content = "Changed question";
        question.Image = "changed.png";
        question.Answers.Clear();
        var usesOptions = type is QuestionType.SingleChoice or QuestionType.MultipleChoice or QuestionType.TrueFalse;
        var request = new SubmitQuizDto { AttemptId = start.AttemptId };
        if (answered)
        {
            List<Guid> selectedIds = [];
            if (type == QuestionType.MultipleChoice)
                selectedIds = [secondAnswer.Id, firstAnswer.Id];
            else if (usesOptions)
                selectedIds = [firstAnswer.Id];

            request.Answers.Add(new SubmitAnswerDto
            {
                QuestionId = question.Id,
                AnswerIds = selectedIds,
                ResponseText = usesOptions ? null : " First answer "
            });
        }

        var submitted = await service.SubmitAsync(quiz.Id, request);
        var reloaded = await service.GetResultAsync(start.AttemptId);

        Assert.Equal(JsonSerializer.Serialize(submitted), JsonSerializer.Serialize(reloaded));
        Assert.Equal(answered ? 100 : 0, submitted.Score);
        Assert.Equal(quiz.PassedScore, submitted.PassedScore);
        var response = Assert.Single(submitted.Answers);
        Assert.Equal(question.Id, response.QuestionId);
        Assert.Equal("Test question", response.QuestionContent);
        Assert.Equal("original.png", response.Image);
        Assert.Equal(QuestionLevel.Medium, response.Level);
        Assert.Equal(type, response.QuestionType);
        Assert.Equal(answered && !usesOptions ? " First answer " : null, response.ResponseText);
        string[] expectedSelections = [];
        if (answered && type == QuestionType.MultipleChoice)
            expectedSelections = ["First answer", "Second answer"];
        else if (answered && usesOptions)
            expectedSelections = ["First answer"];
        Assert.Equal(expectedSelections, response.SelectedAnswers.Select(answer => answer.Text));
    }

    [Fact]
    public async Task Result_includes_the_snapshotted_passed_score()
    {
        using var context = new ServiceTestContext();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        quiz.PassedScore = 70;
        var question = TestEntities.Question(QuestionType.SingleChoice);
        question.IsActive = true;
        question.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, QuestionNavigation = question,
            Text = "Answer", IsCorrect = true, IsActive = true
        });
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        context.Quizzes.Add(quiz);
        var service = context.Get<IQuizAttemptService>();
        var start = await service.StartAsync(quiz.Id);

        var submitted = await service.SubmitAsync(quiz.Id, new SubmitQuizDto {AttemptId = start.AttemptId});
        var reloaded = await service.GetResultAsync(start.AttemptId);

        Assert.Equal(70, submitted.PassedScore);
        Assert.Equal(70, reloaded.PassedScore);
    }
}
