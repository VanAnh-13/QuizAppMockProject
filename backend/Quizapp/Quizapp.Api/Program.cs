using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text;
using System.Security.Claims;
using Quizapp.Api.ExceptionHandlers;
using Quizapp.Application;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.Abstractions.Authentication;
using System.Threading.RateLimiting;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Infrastructure;
using Quizapp.Infrastructure.Authentication;
using Quizapp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOptions<PasswordResetOptions>()
    .Bind(builder.Configuration.GetSection(PasswordResetOptions.SectionName))
    .Validate(options => string.IsNullOrWhiteSpace(options.FrontendUrl)
        || (Uri.TryCreate(options.FrontendUrl, UriKind.Absolute, out var url)
            && string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment)
            && string.IsNullOrEmpty(url.UserInfo)
            && (url.Scheme == Uri.UriSchemeHttps
                || (builder.Environment.IsDevelopment() && url.Scheme == Uri.UriSchemeHttp && url.IsLoopback))),
        "PasswordReset:FrontendUrl must be an HTTPS URL without a query or fragment; local HTTP is allowed in Development.")
    .ValidateOnStart();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    foreach (var (name, limit) in new[] { ("forgot-password", 5), ("reset-password", 10) })
        options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "RATE_LIMITED", message = "Too many requests. Please try again later."
        }, cancellationToken);
    };
});

const string angularDevelopmentCorsPolicy = "AngularDevelopmentClient";

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(angularDevelopmentCorsPolicy, policy => policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
    });
}

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

jwt.Validate();
builder.Services.AddSingleton(Options.Create(jwt));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            RoleClaimType = JwtOptions.RoleClaim
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")
                                  ?.Value
                              ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)
                                  ?.Value;

                var stamp = context.Principal?.FindFirst(JwtOptions.SecurityStampClaim)
                    ?.Value;

                if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty
                                                            || !Guid.TryParse(stamp, out var securityStamp) ||
                                                            securityStamp == Guid.Empty)
                {
                    context.Fail("Invalid access token.");

                    return;
                }

                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

                if (user is null || !user.IsActive || user.SecurityStamp != securityStamp)
                    context.Fail("Invalid access token.");
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.Configure<ContactOptions>(builder.Configuration.GetSection(ContactOptions.SectionName));
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection"),
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(option => option.SwaggerEndpoint("/openapi/v1.json", "QuizApp"));

    if (app.Configuration.GetValue<bool>("SampleData:Enabled"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizAppDbContext>();
        await SampleQuizDataSeeder.SeedAsync(db);
    }
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.UseCors(angularDevelopmentCorsPolicy);
else
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
