using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quizapp.Application.Abstractions.Messaging;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Services.Authentication;
using Quizapp.Application.Services.Common;
using Quizapp.Application.Services.Contact;
using Quizapp.Application.Services.QuestionManager;
using Quizapp.Application.Services.QuizManager;
using Quizapp.Application.Services.QuizTaking;
using Quizapp.Application.Services.RoleManager;
using Quizapp.Application.Services.UserManager;
using Quizapp.Application.Validators.QuizManager.Quizzes;

namespace Quizapp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateQuizDtoValidator>();
        services.AddOptions<ContactOptions>();
        services.AddOptions<PasswordResetOptions>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ServiceAuthorization>();
        services.TryAddScoped<UserProvisioning>();
        services.TryAddScoped<IAuthService, AuthService>();
        services.TryAddScoped<IRoleService, RoleService>();
        services.TryAddScoped<IUserService, UserService>();
        services.TryAddScoped<IQuestionService, QuestionService>();
        services.TryAddScoped<IQuizService, QuizService>();
        services.TryAddScoped<IQuizAttemptService, QuizAttemptService>();
        services.TryAddScoped<IPublicQuizCatalogService, PublicQuizCatalogService>();
        services.TryAddScoped<IContactService, ContactService>();

        return services;
    }
}
