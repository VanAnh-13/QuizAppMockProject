using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quizapp.Application.Factories.QuizManager.Questions;
using Quizapp.Application.Factories.RoleManager;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Application.Validators.QuizManager.Quizzes;
using Quizapp.Application.Services.Common;
using Quizapp.Application.Services.RoleManager;
using Quizapp.Application.Services.Authentication;
using Quizapp.Application.Services.UserManager;

namespace Quizapp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateQuizDtoValidator>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ServiceAuthorization>();
        services.TryAddScoped<IRoleService, RoleService>();
        services.TryAddScoped<UserProvisioning>();
        services.TryAddScoped<IAuthService, AuthService>();
        services.TryAddScoped<IUserService, UserService>();
        services.TryAddScoped<IRoleFactory, RoleFactory>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, SingleChoiceQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, MultipleChoiceQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, TrueFalseQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, TextQuestionStrategy>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IQuestionCreationStrategy, LongAnswerQuestionStrategy>());
        services.TryAddScoped<IQuestionFactory, QuestionFactory>();

        return services;
    }
}
