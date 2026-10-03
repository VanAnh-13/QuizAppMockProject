using System.Net;
using System.Net.Http.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.WebUtilities;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.DTOs.Authentication;
using Quizapp.Domain.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Quizapp.Tests.Api;

public class PasswordResetApiTests
{
    [Fact]
    public async Task Full_queue_rejects_new_requests_without_undoing_a_password_change()
    {
        var email = new BlockingEmailSender();
        await using var factory = new QuizappApiFactory { EmailSender = email };
        using var client = factory.CreateClient();
        try
        {
            using var first = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
            var reset = await ReadResetAsync(email.Delivered);
            var queue = factory.Services.GetRequiredService<IPasswordResetQueue>();
            for (var index = 0; index < 100; index++)
                Assert.True(queue.TryEnqueue(new PasswordResetEmail("unknown@example.com")));

            using var busy = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
            Assert.Equal(HttpStatusCode.ServiceUnavailable, busy.StatusCode);
            using var changed = await client.PostAsJsonAsync("/api/auth/reset-password", reset);
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
            using var login = await client.PostAsJsonAsync("/api/auth/login",
                new { username = factory.User.Username, password = reset.NewPassword });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        }
        finally
        {
            email.Release.TrySetResult();
        }
    }

    private sealed class BlockingEmailSender : IEmailSender
    {
        public ResetEmailSender Delivered { get; } = new();
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task SendAsync(string to, string subject, string body, string? replyTo = null,
            CancellationToken cancellationToken = default)
        {
            await Delivered.SendAsync(to, subject, body, replyTo, cancellationToken);
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    [Fact]
    public async Task Existing_unknown_and_inactive_emails_receive_the_same_response()
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateClient();
        using var existing = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
        using var unknown = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = "unknown@example.com" });
        factory.User.IsActive = false;
        using var inactive = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
        Assert.Equal(HttpStatusCode.Accepted, existing.StatusCode);
        Assert.Equal(existing.StatusCode, unknown.StatusCode);
        Assert.Equal(existing.StatusCode, inactive.StatusCode);
        Assert.Equal(await existing.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Equal(await existing.Content.ReadAsStringAsync(), await inactive.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("forgot-password")]
    [InlineData("reset-password")]
    public async Task Invalid_input_returns_field_errors(string route)
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync($"/api/auth/{route}", new { });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("VALIDATION_ERROR", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("wrong-token")]
    [InlineData("unknown-user")]
    [InlineData("inactive")]
    [InlineData("security-stamp-changed")]
    public async Task Invalid_links_return_the_same_error_and_leave_the_password_unchanged(string reason)
    {
        var email = new ResetEmailSender();
        var clock = new ResetClock();
        await using var factory = new QuizappApiFactory { EmailSender = email, Clock = clock };
        using var client = factory.CreateClient();
        using var issued = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
        var request = await ReadResetAsync(email);
        var password = factory.User.Password;
        switch (reason)
        {
            case "expired": clock.Now = clock.Now.AddMinutes(15); break;
            case "wrong-token": request.Token = "wrong-token"; break;
            case "unknown-user": request.UserId = Guid.NewGuid(); break;
            case "inactive": factory.User.IsActive = false; break;
            case "security-stamp-changed": factory.User.SecurityStamp = Guid.NewGuid(); break;
        }
        using var response = await client.PostAsJsonAsync("/api/auth/reset-password", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("InvalidPasswordResetLink", await response.Content.ReadAsStringAsync());
        Assert.Equal(password, factory.User.Password);
    }

    [Fact]
    public async Task New_link_replaces_the_old_link_after_the_cooldown()
    {
        var email = new ResetEmailSender();
        var clock = new ResetClock();
        await using var factory = new QuizappApiFactory { EmailSender = email, Clock = clock };
        using var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
        var oldRequest = await ReadResetAsync(email);
        clock.Now = clock.Now.AddSeconds(60);
        using var next = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email.ToUpperInvariant() });
        var newRequest = await ReadResetAsync(email);
        using var oldResponse = await client.PostAsJsonAsync("/api/auth/reset-password", oldRequest);
        using var newResponse = await client.PostAsJsonAsync("/api/auth/reset-password", newRequest);
        Assert.Equal(HttpStatusCode.BadRequest, oldResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, newResponse.StatusCode);
    }

    [Theory]
    [InlineData("forgot-password", 5)]
    [InlineData("reset-password", 10)]
    public async Task Excess_requests_are_rate_limited(string route, int limit)
    {
        await using var factory = new QuizappApiFactory();
        using var client = factory.CreateClient();
        for (var index = 0; index < limit; index++)
        {
            using var allowed = await client.PostAsJsonAsync($"/api/auth/{route}", new { });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, allowed.StatusCode);
        }
        using var rejected = await client.PostAsJsonAsync($"/api/auth/{route}", new { });
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Smtp_failure_does_not_reveal_accounts_or_stop_subsequent_delivery()
    {
        var email = new FailOnceEmailSender();
        var clock = new ResetClock();
        await using var factory = new QuizappApiFactory { EmailSender = email, Clock = clock };
        using var client = factory.CreateClient();
        using var first = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        await email.Failed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        clock.Now = clock.Now.AddSeconds(60);
        using var second = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = factory.User.Email });
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        Assert.Contains("Use this link", await email.Delivered.NextAsync());
    }

    private static async Task<ResetPasswordDto> ReadResetAsync(ResetEmailSender email)
    {
        var body = await email.NextAsync();
        var link = new Uri(body.Split('\n').Single(line => line.StartsWith("https://")));
        var parameters = QueryHelpers.ParseQuery(link.Fragment.TrimStart('#'));
        return new ResetPasswordDto
        {
            UserId = Guid.Parse(parameters["userId"].ToString()), Token = parameters["token"].ToString(),
            NewPassword = "New-password-456!", ConfirmNewPassword = "New-password-456!"
        };
    }

    private sealed class ResetClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class FailOnceEmailSender : IEmailSender
    {
        public TaskCompletionSource Failed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ResetEmailSender Delivered { get; } = new();

        public Task SendAsync(string to, string subject, string body, string? replyTo = null,
            CancellationToken cancellationToken = default)
        {
            if (Failed.TrySetResult())
                throw new QuizAppException("Test SMTP failure", "EMAIL_SEND_FAILED");
            return Delivered.SendAsync(to, subject, body, replyTo, cancellationToken);
        }
    }

    [Fact]
    public async Task Email_link_resets_password_once_and_revokes_existing_sessions()
    {
        var email = new ResetEmailSender();
        await using var factory = new QuizappApiFactory { EmailSender = email };
        using var client = factory.CreateClient();
        using var signedIn = factory.CreateAuthenticatedClient();

        using var requested = await client.PostAsJsonAsync("/api/auth/forgot-password",
            new { email = factory.User.Email });
        Assert.Equal(HttpStatusCode.Accepted, requested.StatusCode);
        var body = await email.NextAsync();
        var link = new Uri(body.Split('\n').Single(line => line.StartsWith("https://")));
        Assert.Equal("quizapp.test", link.Host);
        Assert.Empty(link.Query);
        var parameters = QueryHelpers.ParseQuery(link.Fragment.TrimStart('#'));
        var reset = new
        {
            userId = parameters["userId"].ToString(), token = parameters["token"].ToString(),
            newPassword = "New-password-456!", confirmNewPassword = "New-password-456!"
        };

        using var changed = await client.PostAsJsonAsync("/api/auth/reset-password", reset);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        using var repeated = await client.PostAsJsonAsync("/api/auth/reset-password", reset);
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        using var revoked = await signedIn.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var oldLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { username = factory.User.Username, password = QuizappApiFactory.OriginalPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        using var newLogin = await client.PostAsJsonAsync("/api/auth/login",
            new { username = factory.User.Username, password = reset.newPassword });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        Assert.Contains("has been reset", await email.NextAsync());
    }

    internal sealed class ResetEmailSender : IEmailSender
    {
        private readonly Channel<string> _messages = Channel.CreateUnbounded<string>();

        public Task SendAsync(string to, string subject, string body, string? replyTo = null,
            CancellationToken cancellationToken = default)
        {
            _messages.Writer.TryWrite(body);
            return Task.CompletedTask;
        }

        public async Task<string> NextAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            return await _messages.Reader.ReadAsync(timeout.Token);
        }
    }
}
