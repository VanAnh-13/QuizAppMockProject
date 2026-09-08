using System.Net;
using System.Net.Http.Json;
using Quizapp.Application.DTOs.QuizHistory;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Api;

public class QuizAttemptApiTests
{
    [Theory]
    [InlineData(QuestionType.SingleChoice)]
    [InlineData(QuestionType.TrueFalse)]
    public async Task Selecting_all_choices_returns_422_and_allows_a_corrected_submission(QuestionType type)
    {
        await using var factory = new QuizappApiFactory();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var question = TestEntities.Question(type);
        question.IsActive = true;
        var correct = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Correct", IsCorrect = true, IsActive = true
        };
        var wrong = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Wrong", IsCorrect = false, IsActive = true
        };
        question.Answers.Add(correct);
        question.Answers.Add(wrong);
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        factory.Quizzes.Add(quiz);
        using var client = factory.CreateAuthenticatedClient();
        using var startResponse = await client.PostAsync($"/api/quizzes/{quiz.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var start = await startResponse.Content.ReadFromJsonAsync<QuizAttemptStartDto>();
        var request = new SubmitQuizDto
        {
            AttemptId = start!.AttemptId,
            Answers = [new SubmitAnswerDto { QuestionId = question.Id, AnswerIds = [correct.Id, wrong.Id] }]
        };

        using var rejected = await client.PostAsJsonAsync($"/api/quizzes/{quiz.Id}/submit", request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Contains("exactly one active answer", await rejected.Content.ReadAsStringAsync());

        request.Answers.Single().AnswerIds = [correct.Id];
        using var accepted = await client.PostAsJsonAsync($"/api/quizzes/{quiz.Id}/submit", request);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var result = await accepted.Content.ReadFromJsonAsync<QuizAttemptDetailDto>();
        Assert.Equal(start.AttemptId, result!.Id);
        Assert.Equal(100, result.Score);
    }

    [Fact]
    public async Task Start_and_submit_routes_share_an_attempt_and_reject_repeated_submission()
    {
        await using var factory = new QuizappApiFactory();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        factory.Quizzes.Add(quiz);
        using var client = factory.CreateAuthenticatedClient();

        using var startResponse = await client.PostAsync($"/api/quizzes/{quiz.Id}/start", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);
        var start = await startResponse.Content.ReadFromJsonAsync<QuizAttemptStartDto>();
        Assert.NotNull(start);
        Assert.Equal(start.AttemptId, Assert.Single(factory.Attempts.Rows).Id);

        var submission = new SubmitQuizDto { AttemptId = start.AttemptId };
        using var response = await client.PostAsJsonAsync($"/api/quizzes/{quiz.Id}/submit", submission);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<QuizAttemptDetailDto>();
        Assert.Equal(start.AttemptId, result!.Id);

        using var repeated = await client.PostAsJsonAsync($"/api/quizzes/{quiz.Id}/submit", submission);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        using var oldMethod = await client.GetAsync($"/api/quizzes/{quiz.Id}/start");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, oldMethod.StatusCode);
    }

    [Fact]
    public async Task Submit_without_attempt_id_returns_a_validation_error()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.PostAsJsonAsync($"/api/quizzes/{Guid.NewGuid()}/submit", new SubmitQuizDto());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(nameof(SubmitQuizDto.AttemptId), await response.Content.ReadAsStringAsync());
    }
}
