using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Infrastructure.Authentication;

namespace Quizapp.Tests.Api;

public class JwtAuthenticationTests
{
    private const string OriginalPassword = QuizappApiFactory.OriginalPassword;
    private const string ChangedPassword = "Changed-password-456!";
    private const string NextPassword = "Next-password-789!";
    private const string ChangePasswordRoute = "/api/auth/change-password";

    [Theory]
    [InlineData("Issuer", "")]
    [InlineData("Audience", "")]
    [InlineData("SigningKey", "")]
    [InlineData("SigningKey", "too-short")]
    [InlineData("LifetimeMinutes", "0")]
    [InlineData("LifetimeMinutes", "1441")]
    public void Invalid_jwt_configuration_prevents_startup(string setting, string value)
    {
        using var original = new QuizappApiFactory();
        using var factory = original.WithWebHostBuilder(builder =>
            builder.UseSetting($"{JwtOptions.SectionName}:{setting}", value));

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("Configure Jwt issuer", exception.Message);
    }

    [Fact]
    public async Task Password_change_rejects_old_token_and_accepts_new_token()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        using var change = await ChangePasswordAsync(client, OriginalPassword, ChangedPassword);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        using var staleToken = await ChangePasswordAsync(client, ChangedPassword, NextPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, staleToken.StatusCode);
        Assert.Contains(staleToken.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken());
        using var freshToken = await ChangePasswordAsync(client, ChangedPassword, NextPassword);
        Assert.Equal(HttpStatusCode.NoContent, freshToken.StatusCode);
    }

    [Theory]
    [InlineData("stamp-changed")]
    [InlineData("deactivated")]
    [InlineData("deleted")]
    public async Task Token_is_rejected_when_current_user_no_longer_allows_it(string change)
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        switch (change)
        {
            case "stamp-changed":
                factory.User.SecurityStamp = Guid.NewGuid();
                break;
            case "deactivated":
                factory.User.IsActive = false;
                break;
            case "deleted":
                factory.Users.Remove(factory.User);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }

        using var response = await ChangePasswordAsync(client, OriginalPassword, ChangedPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Theory]
    [InlineData(JwtOptions.SecurityStampClaim, null)]
    [InlineData(JwtOptions.SecurityStampClaim, "")]
    [InlineData(JwtOptions.SecurityStampClaim, "not-a-guid")]
    [InlineData(JwtOptions.SecurityStampClaim, "00000000-0000-0000-0000-000000000000")]
    [InlineData(JwtRegisteredClaimNames.Sub, null)]
    [InlineData(JwtRegisteredClaimNames.Sub, "")]
    [InlineData(JwtRegisteredClaimNames.Sub, "not-a-guid")]
    [InlineData(JwtRegisteredClaimNames.Sub, "00000000-0000-0000-0000-000000000000")]
    public async Task Signed_token_with_missing_or_invalid_required_claim_is_rejected(string claim, string? value)
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.CreateTokenWithClaim(claim, value));

        using var response = await ChangePasswordAsync(client, OriginalPassword, ChangedPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    private static Task<HttpResponseMessage> ChangePasswordAsync(HttpClient client, string current, string next) =>
        client.PostAsJsonAsync(ChangePasswordRoute, new ChangePasswordDto
        {
            CurrentPassword = current, NewPassword = next, ConfirmNewPassword = next
        });
}
