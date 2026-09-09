using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Infrastructure.Authentication;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<QuizAppDbContext>(options => options.UseSqlServer(connectionString));

        services.TryAddScoped<IUnitOfWork, EfUnitOfWork>();
        services.TryAddScoped<IUserRepository, EfUserRepository>();
        services.TryAddScoped<IRoleRepository, EfRoleRepository>();
        services.TryAddScoped<IQuizRepository, EfQuizRepository>();
        services.TryAddScoped<IQuestionRepository, EfQuestionRepository>();
        services.TryAddScoped<IQuizAttemptRepository, EfQuizAttemptRepository>();

        services.TryAddScoped<IPasswordService, IdentityPasswordService>();
        services.TryAddScoped<ITokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }
}
