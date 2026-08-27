using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuizApp.Domain.Entities;
using QuizApp.Infrastructure.Persistence;

namespace QuizApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolve the connection string when the context is built (post-Build),
        // so host-level overrides (WebApplicationFactory, containers) apply.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                     ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            options.UseSqlServer(cs);
        });
        services.AddScoped<QuizApp.Application.Interfaces.IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 10;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<QuizApp.Application.Interfaces.IJwtTokenService, Identity.JwtTokenService>();
        services.AddSingleton<QuizApp.Application.Interfaces.IFileStorage, Storage.LocalFileStorage>();

        return services;
    }
}
