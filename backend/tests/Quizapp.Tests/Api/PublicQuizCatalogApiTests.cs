using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Quizapp.Domain.Entities;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Api;

public class PublicQuizCatalogApiTests
{
    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task Public_catalog_returns_only_active_quiz_metadata_without_grading_data(
        bool questionIsActive, int expectedQuestionCount)
    {
        await using var factory = new QuizappApiFactory();
        var activeQuiz = TestEntities.Quiz();
        activeQuiz.IsActive = true;
        var inactiveQuiz = TestEntities.Quiz();
        inactiveQuiz.Title = "Inactive quiz";
        inactiveQuiz.IsActive = false;
        var question = TestEntities.Question();
        question.IsActive = questionIsActive;
        question.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Correct option", IsCorrect = true,
            IsActive = true
        });
        activeQuiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = activeQuiz.Id, QuestionId = question.Id,
            QuizNavigation = activeQuiz, QuestionNavigation = question
        });
        var inactiveQuestion = TestEntities.Question();
        inactiveQuestion.IsActive = false;
        activeQuiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = activeQuiz.Id, QuestionId = inactiveQuestion.Id,
            QuizNavigation = activeQuiz, QuestionNavigation = inactiveQuestion
        });
        factory.Quizzes.Add(activeQuiz);
        factory.Quizzes.Add(inactiveQuiz);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/public/quizzes?pageNumber=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var item = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(activeQuiz.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(activeQuiz.Title, item.GetProperty("title").GetString());
        Assert.Equal(expectedQuestionCount, item.GetProperty("questionCount").GetInt32());
        Assert.DoesNotContain("isCorrect", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_catalog_answers_the_angular_cors_preflight_in_development()
    {
        await using var factory = new QuizappApiFactory
        {
            EnvironmentName = Environments.Development,
            HttpsPort = 7267
        };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/public/quizzes");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:4200",
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Theory]
    [InlineData("Testing", "GET")]
    [InlineData("Testing", "OPTIONS")]
    [InlineData("Staging", "GET")]
    [InlineData("Staging", "OPTIONS")]
    [InlineData("Production", "GET")]
    [InlineData("Production", "OPTIONS")]
    public async Task Public_catalog_redirects_without_development_cors_outside_development(
        string environmentName, string method)
    {
        await using var factory = new QuizappApiFactory { EnvironmentName = environmentName, HttpsPort = 7267 };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(new HttpMethod(method), "/api/public/quizzes?pageNumber=1&pageSize=100");
        request.Headers.Add("Origin", "http://localhost:4200");
        if (request.Method == HttpMethod.Options)
            request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Testing", false)]
    [InlineData("Staging", false)]
    [InlineData("Production", false)]
    public async Task Public_catalog_allows_angular_cors_only_in_development(
        string environmentName, bool corsAllowed)
    {
        await using var factory = new QuizappApiFactory { EnvironmentName = environmentName };
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/public/quizzes");
        request.Headers.Add("Origin", "http://localhost:4200");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(corsAllowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (corsAllowed)
            Assert.Equal("http://localhost:4200",
                Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }
}

