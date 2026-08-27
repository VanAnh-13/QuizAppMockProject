using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace QuizApp.Api.IntegrationTests;

/// <summary>
/// Authorization matrix: POST /api/quizzes without token → 401, with a plain
/// User token → 403, with an Admin token → 201.
/// </summary>
[Collection(ApiCollection.CollectionName)]
public class AuthorizationTests
{
    private readonly QuizApiFactory _factory;

    public AuthorizationTests(QuizApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateQuiz_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/quizzes", new { title = "x", description = "y", duration = 10, isActive = false });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuiz_WithUserToken_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await TestClient.LoginAsync(client, TestClient.DemoUser, TestClient.DemoPassword);
        client.WithBearer(token);

        var response = await client.PostAsJsonAsync("/api/quizzes", new { title = "x", description = "y", duration = 10, isActive = false });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateQuiz_WithAdminToken_Returns201()
    {
        var client = _factory.CreateClient();
        var token = await TestClient.LoginAsync(client, TestClient.AdminUser, TestClient.AdminPassword);
        client.WithBearer(token);

        var title = $"AuthZ quiz {Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/quizzes", new { title, description = "desc", duration = 10, isActive = false });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsFutureExpiry()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { userName = TestClient.AdminUser, password = TestClient.AdminPassword });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
        Assert.True(payload.Expires > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Register_DuplicateUsername_Returns409WithFieldErrors()
    {
        var client = _factory.CreateClient();
        var body = new
        {
            firstName = "Dup",
            lastName = "Licate",
            email = "dup@test.local",
            userName = "dupuser",
            phoneNumber = "0900000000",
            dateOfBirth = "1999-01-01",
            password = "Dup@12345",
            confirmPassword = "Dup@12345"
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", body);
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var problem = await second.Content.ReadFromJsonAsync<ProblemPayload>();
        Assert.NotNull(problem?.Errors);
        Assert.True(problem.Errors.ContainsKey("userName"));
    }

    private sealed class LoginPayload
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset Expires { get; set; }
    }

    public sealed class ProblemPayload
    {
        public string? Title { get; set; }
        public int Status { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
