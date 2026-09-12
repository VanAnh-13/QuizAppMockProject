using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text;
using System.Security.Claims;
using Quizapp.Api.ExceptionHandlers;
using Quizapp.Application;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Infrastructure;
using Quizapp.Infrastructure.Authentication;
using Quizapp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

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
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"));

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
app.UseAuthorization();
app.MapControllers();

app.Run();