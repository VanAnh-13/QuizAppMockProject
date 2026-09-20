using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Quizapp.Api.Controllers;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Domain.Entities;
using Quizapp.Infrastructure.Authentication;
using Quizapp.Tests.Application.Services;

namespace Quizapp.Tests.Api;

internal sealed class QuizappApiFactory : WebApplicationFactory<AuthController>
{
    public const string OriginalPassword = "Original-password-123!";

    private readonly JwtOptions _jwt = new()
    {
        Issuer = "quizapp-authentication-tests",
        Audience = "quizapp-authentication-test-client",
        SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    };

    public User User { get; } = new()
    {
        Id = Guid.NewGuid(),
        Username = "token-user",
        Email = "token-user@example.com",
        Password = new IdentityPasswordService().Hash(OriginalPassword),
        IsActive = true
    };

    public MemoryUsers Users { get; } = new();
    public MemoryQuizzes Quizzes { get; } = new();
    public MemoryQuizAttempts Attempts { get; } = new();
    public MemoryContactMessages ContactMessages { get; } = new();
    public TimeProvider Clock { get; set; } = TimeProvider.System;
    public IEmailSender? EmailSender { get; set; }
    public string EnvironmentName { get; set; } = "Testing";
    public int? HttpsPort { get; set; }

    public QuizappApiFactory() => Users.Add(User);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("SampleData:Enabled", "false");
        if (HttpsPort is not null)
            builder.UseSetting("HTTPS_PORT", HttpsPort.Value.ToString());

        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.UseSetting($"{JwtOptions.SectionName}:Issuer", _jwt.Issuer);
        builder.UseSetting($"{JwtOptions.SectionName}:Audience", _jwt.Audience);
        builder.UseSetting($"{JwtOptions.SectionName}:SigningKey", _jwt.SigningKey);
        builder.UseSetting($"{ContactOptions.SectionName}:InboxAddress", "contact@levananh.dev");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(Clock);
            services.RemoveAll<IUserRepository>();
            services.AddScoped<IUserRepository>(_ => Users);
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork, MemoryUnitOfWork>();
            services.RemoveAll<IContactMessageRepository>();
            services.AddSingleton<IContactMessageRepository>(ContactMessages);
            services.RemoveAll<IQuizRepository>();
            services.AddScoped<IQuizRepository>(_ => Quizzes);
            services.RemoveAll<IQuizAttemptRepository>();
            services.AddScoped<IQuizAttemptRepository>(_ => Attempts);
            services.RemoveAll<IEmailSender>();
            services.AddSingleton(EmailSender ?? new NoOpEmailSender());
        });
    }

    public string CreateToken() =>
        new JwtTokenService(Options.Create(_jwt), TimeProvider.System).Create(User).Value;

    public string CreateTokenWithClaim(string claimType, string? value)
    {
        var handler = new JwtSecurityTokenHandler();
        var original = handler.ReadJwtToken(CreateToken());
        var claims = original.Claims.Where(claim => claim.Type != claimType).ToList();
        if (value is not null)
            claims.Add(new Claim(claimType, value));

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return handler.WriteToken(token);
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken());
        return client;
    }
}
