using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QuizApp.Application.Interfaces;
using QuizApp.Application.Services;

namespace QuizApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
