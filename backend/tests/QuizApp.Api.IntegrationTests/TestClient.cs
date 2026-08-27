using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace QuizApp.Api.IntegrationTests;

[CollectionDefinition(CollectionName)]
public class ApiCollection : ICollectionFixture<QuizApiFactory>
{
    public const string CollectionName = "QuizApi";
}

public static class TestClient
{
    public const string AdminUser = "admin";
    public const string AdminPassword = "Admin@12345";
    public const string DemoUser = "user";
    public const string DemoPassword = "User@12345";

    public static async Task<string> LoginAsync(HttpClient client, string userName, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"Login failed for {userName}: {response.StatusCode} {body}");

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        return payload.Token;
    }

    public static HttpClient WithBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static void ClearBearer(this HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization = null;
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTimeOffset Expires { get; set; }
    }
}
