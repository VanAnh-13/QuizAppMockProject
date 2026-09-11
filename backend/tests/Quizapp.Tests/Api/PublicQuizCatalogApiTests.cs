using System.Net;
using System.Text.Json;
using Quizapp.Domain.Entities;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Api;

public class PublicQuizCatalogApiTests
{
    [Fact]
    public async Task Public_catalog_returns_only_active_quiz_metadata_without_grading_data()
    {
        await using var factory = new QuizappApiFactory();
        var activeQuiz = TestEntities.Quiz();
        activeQuiz.IsActive = true;
        var inactiveQuiz = TestEntities.Quiz();
        inactiveQuiz.Title = "Inactive quiz";
        inactiveQuiz.IsActive = false;
        var question = TestEntities.Question();
        question.IsActive = true;
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
        Assert.Equal(1, item.GetProperty("questionCount").GetInt32());
        Assert.DoesNotContain("isCorrect", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", payload, StringComparison.OrdinalIgnoreCase);
    }
}
