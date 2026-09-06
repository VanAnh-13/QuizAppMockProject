using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string? connectionString)
    {
        services.AddDbContext<QuizAppDbContext>(options => options.UseSqlServer(connectionString));

        return services;
    }
}
