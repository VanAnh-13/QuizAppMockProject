using System.Net;
using System.Net.Http.Json;
using Quizapp.Api.ExceptionHandlers;

namespace Quizapp.Tests.Api;

public class AuthValidationApiTests
{
    [Fact]
    public async Task Invalid_registration_returns_422_with_field_validation_errors()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new { });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Error);
        Assert.NotNull(error.Errors);
        foreach (var field in new[] { "Username", "Email", "Password", "ConfirmPassword", "Profile.FullName" })
        {
            Assert.True(error.Errors.TryGetValue(field, out var messages), $"Missing validation errors for {field}.");
            Assert.NotEmpty(messages);
        }
    }
}
