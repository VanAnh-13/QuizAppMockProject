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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwt.Validate();
builder.Services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwt));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // preserve short claim names ("role", "sub") as issued
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
                var subject = context.Principal?.FindFirst("sub")?.Value
                              ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var stamp = context.Principal?.FindFirst(JwtOptions.SecurityStampClaim)?.Value;

                if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty
                    || !Guid.TryParse(stamp, out var securityStamp) || securityStamp == Guid.Empty)
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
}

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
