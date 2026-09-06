using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application.Validators.QuizManager.Quizzes;

namespace Quizapp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateQuizDtoValidator>();

        return services;
    }
}
