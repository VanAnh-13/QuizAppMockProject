using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;

namespace Quizapp.Tests.Api;

public class ForwardedHeadersApiTests
{
    [Theory]
    [InlineData("forgot-password", 5, false)]
    [InlineData("reset-password", 10, false)]
    [InlineData("forgot-password", 5, true)]
    [InlineData("reset-password", 10, true)]
    public async Task Trusted_proxy_clients_have_separate_rate_limits(string route, int limit, bool network)
    {
        await using var factory = new QuizappApiFactory();
        await using var host = factory.WithWebHostBuilder(builder => builder.UseSetting(
            network ? "ForwardedHeaders:KnownIPNetworks:0" : "ForwardedHeaders:KnownProxies:0",
            network ? "10.0.0.0/24" : "10.0.0.10"));

        for (var index = 0; index < limit; index++)
        {
            var allowed = await SendAsync(host.Server, route, "10.0.0.10", "203.0.113.1");
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, allowed.Response.StatusCode);
        }

        var rejected = await SendAsync(host.Server, route, "10.0.0.10", "203.0.113.1");
        Assert.Equal(StatusCodes.Status429TooManyRequests, rejected.Response.StatusCode);
        Assert.True(rejected.Response.Headers.ContainsKey("Retry-After"));

        var otherClient = await SendAsync(host.Server, route, "10.0.0.10", "203.0.113.2");
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, otherClient.Response.StatusCode);
        Assert.Equal(IPAddress.Parse("203.0.113.2"), otherClient.Connection.RemoteIpAddress);
    }

    [Theory]
    [InlineData("forgot-password", 5, false)]
    [InlineData("reset-password", 10, false)]
    [InlineData("forgot-password", 5, true)]
    [InlineData("reset-password", 10, true)]
    public async Task Untrusted_clients_cannot_evade_limits_by_changing_forwarded_headers(
        string route, int limit, bool configureProxy)
    {
        await using var factory = new QuizappApiFactory();
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            if (configureProxy)
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.10");
        });

        for (var index = 0; index < limit; index++)
        {
            var allowed = await SendAsync(host.Server, route, "198.51.100.10", $"203.0.113.{index + 1}");
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, allowed.Response.StatusCode);
        }

        var rejected = await SendAsync(host.Server, route, "198.51.100.10", "203.0.113.100");
        Assert.Equal(StatusCodes.Status429TooManyRequests, rejected.Response.StatusCode);
        Assert.Equal(IPAddress.Parse("198.51.100.10"), rejected.Connection.RemoteIpAddress);
    }

    [Theory]
    [InlineData("forgot-password", 5)]
    [InlineData("reset-password", 10)]
    public async Task Spoofed_header_prefix_does_not_change_the_verified_client(string route, int limit)
    {
        await using var factory = new QuizappApiFactory();
        await using var host = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.10"));

        for (var index = 0; index <= limit; index++)
        {
            var response = await SendAsync(host.Server, route, "10.0.0.10", $"203.0.113.{index + 1}, 198.51.100.10");
            Assert.Equal(index < limit ? StatusCodes.Status422UnprocessableEntity : StatusCodes.Status429TooManyRequests,
                response.Response.StatusCode);
            Assert.Equal(IPAddress.Parse("198.51.100.10"), response.Connection.RemoteIpAddress);
        }
    }

    [Theory]
    [InlineData("10.0.0.10", StatusCodes.Status422UnprocessableEntity)]
    [InlineData("198.51.100.10", StatusCodes.Status307TemporaryRedirect)]
    public async Task Only_trusted_proxies_can_forward_https_before_redirection(string peer, int expectedStatus)
    {
        await using var factory = new QuizappApiFactory { HttpsPort = 443 };
        await using var host = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.10"));

        var response = await SendAsync(host.Server, "forgot-password", peer, "203.0.113.1", "http");

        Assert.Equal(expectedStatus, response.Response.StatusCode);
    }

    [Theory]
    [InlineData("10.0.0.10", "203.0.113.1, 10.0.0.11", "203.0.113.1")]
    [InlineData("::ffff:10.0.0.10", "203.0.113.1, 10.0.0.11", "203.0.113.1")]
    [InlineData("10.0.0.10", "203.0.113.1, 198.51.100.10", "198.51.100.10")]
    public async Task Multiple_hops_stop_at_the_first_untrusted_address(string peer, string forwardedFor,
        string expectedClient)
    {
        await using var factory = new QuizappApiFactory();
        await using var host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "10.0.0.10");
            builder.UseSetting("ForwardedHeaders:KnownProxies:1", "10.0.0.11");
            builder.UseSetting("ForwardedHeaders:ForwardLimit", "2");
        });

        var response = await SendAsync(host.Server, "forgot-password", peer, forwardedFor);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, response.Response.StatusCode);
        Assert.Equal(IPAddress.Parse(expectedClient), response.Connection.RemoteIpAddress);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Invalid_forward_limits_fail_at_startup(string limit)
    {
        await using var factory = new QuizappApiFactory();
        await using var host = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ForwardedHeaders:ForwardLimit", limit));

        Assert.Throws<OptionsValidationException>(() => host.CreateClient());
    }

    private static Task<HttpContext> SendAsync(TestServer server, string route, string peer, string forwardedFor,
        string scheme = "https") => server.SendAsync(context =>
    {
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Method = HttpMethods.Post;
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString("localhost");
        context.Request.Path = $"/api/auth/{route}";
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        context.Request.ContentLength = context.Request.Body.Length;
    });
}
