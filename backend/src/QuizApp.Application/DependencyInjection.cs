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
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAnswerService, AnswerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        // T4: quiz-taking services
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<IQuizCodeService, QuizCodeService>();
        services.AddScoped<IQuizAttemptService, QuizAttemptService>();

        return services;
    }
}
