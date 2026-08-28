using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace QuizApp.Api.IntegrationTests;

/// <summary>
/// Integration tests for the quiz-taking journey (T4):
/// register → login → self-issue code → prepare → take → submit → history.
/// Also covers security requirements: no isCorrect in take response, repeat-submit → 409.
/// </summary>
[Collection(ApiCollection.CollectionName)]
public class QuizTakingTests
{
    private readonly QuizApiFactory _factory;

    public QuizTakingTests(QuizApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private async Task<(HttpClient client, string token, string userId)> RegisterAndLoginAsync(string? suffix = null)
    {
        suffix ??= Guid.NewGuid().ToString("N")[..8];
        var client = CreateClient();

        var reg = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = $"test_{suffix}@example.com",
            userName = $"testuser_{suffix}",
            phoneNumber = "0123456789",
            dateOfBirth = "2000-01-01",
            password = "Test@12345",
            confirmPassword = "Test@12345",
            isActive = true
        });
        reg.EnsureSuccessStatusCode();

        var token = await TestClient.LoginAsync(client, $"testuser_{suffix}", "Test@12345");
        var meResp = await client.WithBearer(token).GetAsync("/api/auth/me");
        meResp.EnsureSuccessStatusCode();
        var me = await meResp.Content.ReadFromJsonAsync<UserDto>();

        return (client.WithBearer(token), token, me!.Id);
    }

    private async Task<(string quizId, string questionId)> CreateQuizWithSingleChoiceQuestionAsync(HttpClient adminClient)
    {
        var quizResp = await adminClient.PostAsJsonAsync("/api/quizzes", new
        {
            title = $"Test Quiz {Guid.NewGuid():N}",
            description = "Integration test quiz",
            duration = 30,
            isActive = false
        });
        quizResp.EnsureSuccessStatusCode();
        var quiz = await quizResp.Content.ReadFromJsonAsync<QuizDto>();

        var qResp = await adminClient.PostAsJsonAsync("/api/questions", new
        {
            content = "What is 1 + 1?",
            questionType = 1, // SingleChoice
            isActive = true
        });
        qResp.EnsureSuccessStatusCode();
        var question = await qResp.Content.ReadFromJsonAsync<QuestionDto>();

        // Add answers
        var ans1 = await adminClient.PostAsJsonAsync($"/api/questions/{question!.Id}/answers", new
        {
            content = "2",
            isCorrect = true,
            isActive = true,
            questionId = question.Id
        });
        ans1.EnsureSuccessStatusCode();
        await adminClient.PostAsJsonAsync($"/api/questions/{question.Id}/answers", new
        {
            content = "3",
            isCorrect = false,
            isActive = true,
            questionId = question.Id
        });

        // Assign question to quiz
        await adminClient.PostAsJsonAsync($"/api/quizzes/{quiz!.Id}/questions", new
        {
            quizId = quiz.Id,
            questionId = question.Id
        });

        // Activate quiz
        await adminClient.PutAsJsonAsync($"/api/quizzes/{quiz.Id}", new
        {
            id = quiz.Id,
            title = quiz.Title,
            description = quiz.Description,
            duration = quiz.Duration,
            isActive = true
        });

        return (quiz.Id, question.Id);
    }

    // ---- Full happy path -----------------------------------------------

    [Fact]
    public async Task FullJourney_RegisterLoginCodePrepareTakeSubmit_ScoreReturned()
    {
        // Arrange: admin creates quiz + question
        var adminClient = CreateClient().WithBearer(
            await TestClient.LoginAsync(CreateClient(), TestClient.AdminUser, TestClient.AdminPassword));
        var (quizId, questionId) = await CreateQuizWithSingleChoiceQuestionAsync(adminClient);

        // Get the correct answer id
        var answersResp = await adminClient.GetAsync($"/api/questions/{questionId}/answers");
        answersResp.EnsureSuccessStatusCode();
        var answers = await answersResp.Content.ReadFromJsonAsync<AnswerDto[]>();
        var correctAnswerId = answers!.First(a => a.IsCorrect).Id;

        // User registers and logs in
        var (userClient, _, userId) = await RegisterAndLoginAsync();

        // Self-issue a code (Start button)
        var codeResp = await userClient.PostAsync($"/api/quizzes/{quizId}/codes/self", null);
        codeResp.EnsureSuccessStatusCode();
        var codeResult = await codeResp.Content.ReadFromJsonAsync<SelfIssueCodeDto>();
        var code = codeResult!.QuizCode;
        Assert.NotEmpty(code);

        // Prepare
        var prepareResp = await userClient.PostAsJsonAsync("/api/quizzes/prepare", new
        {
            userId,
            quizId,
            quizCode = code
        });
        prepareResp.EnsureSuccessStatusCode();
        var prepareInfo = await prepareResp.Content.ReadFromJsonAsync<PrepareDtoJson>();
        Assert.Equal(quizId, prepareInfo!.Id);

        // Take
        var takeResp = await userClient.PostAsJsonAsync("/api/quizzes/take", new
        {
            userId,
            quizId,
            quizCode = code
        });
        takeResp.EnsureSuccessStatusCode();

        // --- Critical security assertion: isCorrect must not appear in the raw JSON ---
        var takeJson = await takeResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain("isCorrect", takeJson, StringComparison.OrdinalIgnoreCase);

        var takeData = JsonSerializer.Deserialize<QuizForTestDtoJson>(takeJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        Assert.NotNull(takeData);
        Assert.NotEmpty(takeData.Questions);
        Assert.NotNull(takeData.StartTime);
        Assert.NotNull(takeData.EndTime);

        var question = takeData.Questions[0];

        // Submit with the correct answer
        var submitResp = await userClient.PostAsJsonAsync("/api/quizzes/submit", new
        {
            quizId,
            userId,
            quizCode = code,
            answers = new[]
            {
                new { questionId = question.Id, answerId = correctAnswerId }
            }
        });
        submitResp.EnsureSuccessStatusCode();
        var result = await submitResp.Content.ReadFromJsonAsync<QuizResultDto>();
        Assert.NotNull(result);
        Assert.Equal(100, result.Score);
        Assert.Equal(1, result.CorrectCount);
        Assert.Equal(1, result.TotalQuestions);

        // Verify history
        var historyResp = await userClient.GetAsync("/api/attempts/me");
        historyResp.EnsureSuccessStatusCode();
        var history = await historyResp.Content.ReadFromJsonAsync<AttemptSummaryDto[]>();
        Assert.NotNull(history);
        Assert.Contains(history, a => a.QuizId == quizId);
    }

    // ---- Repeat submit returns 409 -------------------------------------

    [Fact]
    public async Task Submit_Twice_Returns409()
    {
        var adminClient = CreateClient().WithBearer(
            await TestClient.LoginAsync(CreateClient(), TestClient.AdminUser, TestClient.AdminPassword));
        var (quizId, _) = await CreateQuizWithSingleChoiceQuestionAsync(adminClient);

        var (userClient, _, userId) = await RegisterAndLoginAsync();

        // Self-issue code
        var codeResp = await userClient.PostAsync($"/api/quizzes/{quizId}/codes/self", null);
        var code = (await codeResp.Content.ReadFromJsonAsync<SelfIssueCodeDto>())!.QuizCode;

        await userClient.PostAsJsonAsync("/api/quizzes/prepare", new { userId, quizId, quizCode = code });
        await userClient.PostAsJsonAsync("/api/quizzes/take", new { userId, quizId, quizCode = code });

        // First submit
        var submit1 = await userClient.PostAsJsonAsync("/api/quizzes/submit", new
        {
            quizId, userId, quizCode = code, answers = Array.Empty<object>()
        });
        submit1.EnsureSuccessStatusCode();

        // Second submit must be 409
        var submit2 = await userClient.PostAsJsonAsync("/api/quizzes/submit", new
        {
            quizId, userId, quizCode = code, answers = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.Conflict, submit2.StatusCode);
    }

    // ---- Code belonging to another user is rejected --------------------

    [Fact]
    public async Task PrepareWithAnotherUserCode_Returns403()
    {
        var adminClient = CreateClient().WithBearer(
            await TestClient.LoginAsync(CreateClient(), TestClient.AdminUser, TestClient.AdminPassword));
        var (quizId, _) = await CreateQuizWithSingleChoiceQuestionAsync(adminClient);

        // User1 gets a code
        var (user1Client, _, user1Id) = await RegisterAndLoginAsync();
        var codeResp = await user1Client.PostAsync($"/api/quizzes/{quizId}/codes/self", null);
        var code = (await codeResp.Content.ReadFromJsonAsync<SelfIssueCodeDto>())!.QuizCode;

        // User2 tries to use User1's code
        var (user2Client, _, user2Id) = await RegisterAndLoginAsync();
        var prepareResp = await user2Client.PostAsJsonAsync("/api/quizzes/prepare", new
        {
            userId = user2Id,
            quizId,
            quizCode = code
        });
        Assert.Equal(HttpStatusCode.Forbidden, prepareResp.StatusCode);
    }

    // ---- Attempt detail only visible to owner or manager ---------------

    [Fact]
    public async Task AttemptDetail_OtherUser_Returns403()
    {
        var adminClient = CreateClient().WithBearer(
            await TestClient.LoginAsync(CreateClient(), TestClient.AdminUser, TestClient.AdminPassword));
        var (quizId, _) = await CreateQuizWithSingleChoiceQuestionAsync(adminClient);

        var (user1Client, _, user1Id) = await RegisterAndLoginAsync();
        var codeResp = await user1Client.PostAsync($"/api/quizzes/{quizId}/codes/self", null);
        var code = (await codeResp.Content.ReadFromJsonAsync<SelfIssueCodeDto>())!.QuizCode;
        await user1Client.PostAsJsonAsync("/api/quizzes/prepare", new { userId = user1Id, quizId, quizCode = code });
        await user1Client.PostAsJsonAsync("/api/quizzes/take", new { userId = user1Id, quizId, quizCode = code });
        var submitResp = await user1Client.PostAsJsonAsync("/api/quizzes/submit", new
        {
            quizId, userId = user1Id, quizCode = code, answers = Array.Empty<object>()
        });
        var result = await submitResp.Content.ReadFromJsonAsync<QuizResultDto>();

        // User2 cannot see User1's attempt detail
        var (user2Client, _, _) = await RegisterAndLoginAsync();
        var detailResp = await user2Client.GetAsync($"/api/attempts/{result!.AttemptId}");
        Assert.Equal(HttpStatusCode.Forbidden, detailResp.StatusCode);
    }
}

// ---- Local DTO helpers for deserialization ----------------------------

file sealed class UserDto { public string Id { get; set; } = string.Empty; }
file sealed class QuizDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsActive { get; set; }
}
file sealed class QuestionDto { public string Id { get; set; } = string.Empty; }
file sealed class AnswerDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
file sealed class SelfIssueCodeDto { public string QuizCode { get; set; } = string.Empty; }
file sealed class PrepareDtoJson { public string Id { get; set; } = string.Empty; }
file sealed class QuizForTestDtoJson
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public QuestionForTestDtoJson[] Questions { get; set; } = [];
}
file sealed class QuestionForTestDtoJson { public string Id { get; set; } = string.Empty; }
file sealed class QuizResultDto
{
    public string AttemptId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
}
file sealed class AttemptSummaryDto { public string QuizId { get; set; } = string.Empty; }
