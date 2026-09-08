using System.Net;
using System.Net.Http.Json;
using Quizapp.Domain.Entities;

namespace Quizapp.Tests.Api;

public class SetActiveApiTests
{
    private static HttpClient CreateAdminClient(QuizappApiFactory factory)
    {
        // Add an active Admin role to the factory user so CreateToken() includes the role claim.
        factory.User.Roles.Add(new Role { Id = Guid.NewGuid(), RoleName = "Admin", IsActive = true });
        return factory.CreateAuthenticatedClient();
    }

    [Theory]
    [InlineData("/api/quizzes")]
    [InlineData("/api/questions")]
    [InlineData("/api/users")]
    public async Task Activate_without_isActive_field_returns_400(string route)
    {
        await using var factory = new QuizappApiFactory();
        using var client = CreateAdminClient(factory);

        using var response = await client.PatchAsJsonAsync(
            $"{route}/{Guid.NewGuid()}/activate",
            new { }); // no isActive field — should fail model validation

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
