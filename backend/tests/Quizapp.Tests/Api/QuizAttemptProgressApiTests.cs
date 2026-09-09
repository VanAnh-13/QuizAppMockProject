using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Quizapp.Application.DTOs.QuizTaking;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Api;

public class QuizAttemptProgressApiTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(30)]
    public async Task Frontend_can_submit_saved_answers_before_at_or_after_expiry(int secondsFromExpiry)
    {
        var clock = new ProgressClock();
        await using var factory = new QuizappApiFactory { Clock = clock };
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var question = TestEntities.Question(QuestionType.ShortAnswer);
        question.IsActive = true;
        question.Answers.Add(new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Sample", IsCorrect = true, IsActive = true
        });
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        factory.Quizzes.Add(quiz);
        using var client = factory.CreateAuthenticatedClient();
        using var started = await client.PostAsync($"/api/quizzes/{quiz.Id}/start", null);
        var start = (await started.Content.ReadFromJsonAsync<QuizAttemptStartDto>())!;
        using var saved = await client.PutAsJsonAsync($"/api/attempts/{start.AttemptId}/progress", new
        {
            revision = 0,
            answers = new[] { new SubmitAnswerDto { QuestionId = question.Id, ResponseText = " sample " } }
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        clock.Now = new DateTimeOffset(start.ExpiresAt.AddSeconds(secondsFromExpiry));

        using var submitted = await client.PostAsJsonAsync($"/api/attempts/{start.AttemptId}/submit", new { revision = 1 });

        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var result = await submitted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, result.GetProperty("score").GetDouble());
        Assert.Equal(start.AttemptId, result.GetProperty("id").GetGuid());
        using var repeated = await client.PostAsJsonAsync($"/api/attempts/{start.AttemptId}/submit", new { revision = 1 });
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
    }

    [Fact]
    public async Task In_progress_list_finds_unfinished_attempts_and_excludes_submitted_attempts()
    {
        await using var factory = new QuizappApiFactory();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        factory.Quizzes.Add(quiz);
        using var client = factory.CreateAuthenticatedClient();
        using var started = await client.PostAsync($"/api/quizzes/{quiz.Id}/start", null);
        var start = (await started.Content.ReadFromJsonAsync<QuizAttemptStartDto>())!;

        using var listed = await client.GetAsync("/api/attempts/in-progress?pageNumber=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        var page = await listed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(start.AttemptId, page.GetProperty("items")[0].GetProperty("attemptId").GetGuid());

        using var submitted = await client.PostAsJsonAsync($"/api/quizzes/{quiz.Id}/submit",
            new SubmitQuizDto { AttemptId = start.AttemptId });
        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var remaining = await client.GetFromJsonAsync<JsonElement>("/api/attempts/in-progress");
        Assert.Equal(0, remaining.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Pausing_freezes_time_and_resuming_keeps_the_same_attempt_and_remaining_time()
    {
        var clock = new ProgressClock();
        await using var factory = new QuizappApiFactory { Clock = clock };
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        quiz.Duration = 10;
        factory.Quizzes.Add(quiz);
        using var client = factory.CreateAuthenticatedClient();
        using var started = await client.PostAsync($"/api/quizzes/{quiz.Id}/start", null);
        var start = (await started.Content.ReadFromJsonAsync<QuizAttemptStartDto>())!;
        clock.Now = new DateTimeOffset(2026, 9, 9, 10, 2, 0, TimeSpan.Zero);

        using var paused = await client.PostAsJsonAsync($"/api/attempts/{start.AttemptId}/pause",
            new { revision = 0, answers = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.OK, paused.StatusCode);
        clock.Now = new DateTimeOffset(2026, 9, 9, 13, 2, 0, TimeSpan.Zero);
        var progress = await client.GetFromJsonAsync<JsonElement>($"/api/attempts/{start.AttemptId}/progress");
        Assert.Equal(480, progress.GetProperty("remainingSeconds").GetDouble());
        Assert.Equal(JsonValueKind.String, progress.GetProperty("pausedAt").ValueKind);

        using var resumed = await client.PostAsJsonAsync($"/api/attempts/{start.AttemptId}/resume", new { revision = 1 });
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        var resumedState = await resumed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(start.AttemptId, resumedState.GetProperty("attemptId").GetGuid());
        Assert.Equal(480, resumedState.GetProperty("remainingSeconds").GetDouble());
        Assert.Equal(JsonValueKind.Null, resumedState.GetProperty("pausedAt").ValueKind);
        Assert.Equal(new DateTime(2026, 9, 9, 13, 10, 0, DateTimeKind.Utc),
            resumedState.GetProperty("expiresAt").GetDateTime());
    }

    [Fact]
    public async Task Saved_answers_can_be_loaded_in_a_new_request_and_cleared_when_deselected()
    {
        await using var factory = new QuizappApiFactory();
        var quiz = TestEntities.Quiz();
        quiz.IsActive = true;
        var question = TestEntities.Question(QuestionType.SingleChoice);
        question.IsActive = true;
        var answer = new Answer
        {
            Id = Guid.NewGuid(), QuestionId = question.Id, Text = "Option", IsCorrect = true, IsActive = true
        };
        question.Answers.Add(answer);
        quiz.QuizQuestions.Add(new QuizQuestion
        {
            Id = Guid.NewGuid(), QuizId = quiz.Id, QuestionId = question.Id,
            QuizNavigation = quiz, QuestionNavigation = question
        });
        factory.Quizzes.Add(quiz);
        using var client = factory.CreateAuthenticatedClient();
        using var started = await client.PostAsync($"/api/quizzes/{quiz.Id}/start", null);
        var start = (await started.Content.ReadFromJsonAsync<QuizAttemptStartDto>())!;
        var route = $"/api/attempts/{start.AttemptId}/progress";

        using var saved = await client.PutAsJsonAsync(route, new
        {
            revision = 0,
            answers = new[] { new SubmitAnswerDto { QuestionId = question.Id, AnswerIds = [answer.Id] } }
        });
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        var progress = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Equal(answer.Id, progress.GetProperty("answers")[0].GetProperty("answerIds")[0].GetGuid());
        Assert.Equal(1, progress.GetProperty("revision").GetInt64());
        Assert.False(progress.TryGetProperty("score", out _));

        using var cleared = await client.PutAsJsonAsync(route, new { revision = 1, answers = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
        var reloaded = await client.GetFromJsonAsync<JsonElement>(route);
        Assert.Empty(reloaded.GetProperty("answers").EnumerateArray());
    }

    private sealed class ProgressClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 9, 10, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
