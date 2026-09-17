using System.Net;
using System.Text.Json;

namespace Quizapp.Tests.Api;

public class CurrentUserApiTests
{
    private const string CurrentUserRoute = "/api/auth/me";

    [Fact]
    public async Task Current_user_route_returns_the_signed_in_profile_without_the_password()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        using var response = await client.GetAsync(CurrentUserRoute);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var profile = payload.RootElement;

        Assert.Equal(factory.User.Id, profile.GetProperty("id").GetGuid());
        Assert.Equal(factory.User.Username, profile.GetProperty("username").GetString());
        Assert.Equal(factory.User.Email, profile.GetProperty("email").GetString());

        Assert.DoesNotContain(profile.EnumerateObject(),
            property => property.Name.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Current_user_route_rejects_anonymous_callers()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(CurrentUserRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
