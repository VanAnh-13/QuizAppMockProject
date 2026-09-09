using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quizapp.Application;
using Quizapp.Application.Abstractions.Authentication;
using Quizapp.Application.Abstractions.Persistence;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.Factories.QuizManager.Questions;
using Quizapp.Application.Factories.RoleManager;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Domain.Enums;
using Quizapp.Infrastructure;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Tests.Application.Services;
using Quizapp.Tests.Data;

namespace Quizapp.Tests.Architecture;

public class DependencyInjectionTests
{
    // Registers no-op stubs for all infrastructure-provided contracts so
    // AddApplication() can be validated without a real database or HTTP context.
    private static void AddStubs(IServiceCollection services)
    {
        services.TryAddSingleton<ICurrentUser, TestCurrentUser>();
        services.TryAddSingleton<MemoryUsers>();
        services.TryAddSingleton<IUserRepository>(sp => sp.GetRequiredService<MemoryUsers>());

        services.TryAddSingleton<IRoleRepository>(sp =>
            new MemoryRoles(sp.GetRequiredService<MemoryUsers>()));

        services.TryAddSingleton<IQuizRepository, MemoryQuizzes>();
        services.TryAddSingleton<IQuestionRepository, MemoryQuestions>();
        services.TryAddSingleton<IQuizAttemptRepository, MemoryQuizAttempts>();
        services.TryAddSingleton<IUnitOfWork, MemoryUnitOfWork>();
        services.TryAddSingleton<IPasswordService, NullPasswordService>();
        services.TryAddSingleton<ITokenService, NullTokenService>();
    }

    [Fact]
    public async Task Repository_stubs_resolve_and_share_one_user_store_after_repeated_registration()
    {
        var services = new ServiceCollection();
        AddStubs(services);
        AddStubs(services);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        var roles = provider.GetRequiredService<IRoleRepository>();
        var users = provider.GetRequiredService<IUserRepository>();
        Assert.Same(provider.GetRequiredService<MemoryUsers>(), users);

        var role = TestEntities.Role();
        var user = TestEntities.User();
        user.Roles.Add(role);
        Assert.False(await roles.HasUsersAsync(role.Id, CancellationToken.None));

        users.Add(user);

        Assert.True(await roles.HasUsersAsync(role.Id, CancellationToken.None));
        users.Remove(user);
        Assert.False(await roles.HasUsersAsync(role.Id, CancellationToken.None));
    }

    [Fact]
    public void Creation_factories_and_strategies_are_scoped_and_registration_can_be_repeated()
    {
        var services = new ServiceCollection().AddApplication()
            .AddApplication();

        AddStubs(services);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        foreach (var contract in new[] { typeof(IRoleFactory), typeof(IQuestionFactory) })
        {
            var first = firstScope.ServiceProvider.GetRequiredService(contract);
            Assert.Same(first, firstScope.ServiceProvider.GetRequiredService(contract));
            Assert.NotSame(first, secondScope.ServiceProvider.GetRequiredService(contract));
        }

        var strategies = firstScope.ServiceProvider.GetServices<IQuestionCreationStrategy>()
            .ToArray();

        var registeredTypes = strategies.SelectMany(strategy => strategy.SupportedTypes)
            .ToArray();

        Assert.Equal(Enum.GetValues<QuestionType>()
            .Order(), registeredTypes.Order());

        foreach (var strategy in strategies)
        {
            Assert.Contains(firstScope.ServiceProvider.GetServices<IQuestionCreationStrategy>(),
                other => ReferenceEquals(strategy, other));

            Assert.DoesNotContain(secondScope.ServiceProvider.GetServices<IQuestionCreationStrategy>(),
                other => ReferenceEquals(strategy, other));
        }
    }

    [Fact]
    public void Application_registration_resolves_every_validator_with_scoped_lifetime()
    {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddApplication());
        AddStubs(services);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var validatorTypes = typeof(CreateQuizDto).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                           && typeof(IValidator).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(validatorTypes);

        foreach (var validatorType in validatorTypes)
        {
            var contract = Assert.Single(validatorType.GetInterfaces(), type =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValidator<>));

            var firstValidator = firstScope.ServiceProvider.GetRequiredService(contract);

            Assert.Equal(validatorType, firstValidator.GetType());
            Assert.Same(firstValidator, firstScope.ServiceProvider.GetRequiredService(contract));
            Assert.NotSame(firstValidator, secondScope.ServiceProvider.GetRequiredService(contract));
        }
    }

    [Fact]
    public void Application_registration_resolves_the_quiz_validator_from_its_own_assembly()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetRequiredService<IValidator<CreateQuizDto>>();

        var result = validator.Validate(new CreateQuizDto { Title = "", Duration = 0, PassedScore = 0 });

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateQuizDto.Title));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateQuizDto.Duration));

        Assert.True(validator.Validate(new CreateQuizDto { Title = "Sample quiz", Duration = 15, PassedScore = 5 })
            .IsValid);
    }

    [Fact]
    public void Infrastructure_registration_uses_a_scoped_sql_server_context()
    {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddInfrastructure(connectionString: null));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstContext = firstScope.ServiceProvider.GetRequiredService<QuizAppDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<QuizAppDbContext>();

        Assert.True(firstContext.Database.IsSqlServer());
        Assert.Same(firstContext, firstScope.ServiceProvider.GetRequiredService<QuizAppDbContext>());
        Assert.NotSame(firstContext, secondContext);
    }
}

// Minimal no-op implementations for IPasswordService and ITokenService
// used only in DI validation tests — they never process real credentials.
file sealed class NullPasswordService : IPasswordService
{
    public string Hash(string password) => password;

    public bool Verify(string hash, string password) => false;
}

file sealed class NullTokenService : ITokenService
{
    public AccessToken Create(Quizapp.Domain.Entities.User user) =>
        new(string.Empty, DateTime.MinValue);
}
