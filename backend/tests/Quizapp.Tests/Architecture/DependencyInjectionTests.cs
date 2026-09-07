using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application;
using Quizapp.Application.DTOs.QuizManager.Quizzes;
using Quizapp.Application.Strategies.QuizManager.Questions;
using Quizapp.Domain.Enums;
using Quizapp.Infrastructure;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Architecture;

public class DependencyInjectionTests
{
    [Fact]
    public void Question_strategies_are_scoped_and_registration_can_be_repeated()
    {
        var services = new ServiceCollection().AddApplication().AddApplication();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var strategies = firstScope.ServiceProvider.GetServices<IQuestionCreationStrategy>().ToArray();
        var registeredTypes = strategies.SelectMany(strategy => strategy.SupportedTypes).ToArray();
        Assert.Equal(Enum.GetValues<QuestionType>().Order(), registeredTypes.Order());
        foreach (var strategy in strategies)
        {
            Assert.Contains(firstScope.ServiceProvider.GetServices<IQuestionCreationStrategy>(), other => ReferenceEquals(strategy, other));
            Assert.DoesNotContain(secondScope.ServiceProvider.GetServices<IQuestionCreationStrategy>(), other => ReferenceEquals(strategy, other));
        }
    }

    [Fact]
    public void Application_registration_resolves_every_validator_with_scoped_lifetime()
    {
        var services = new ServiceCollection();
        Assert.Same(services, services.AddApplication());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var validatorTypes = typeof(CreateQuizDto).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IValidator).IsAssignableFrom(type)).ToArray();

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
        Assert.True(validator.Validate(new CreateQuizDto { Title = "Sample quiz", Duration = 15, PassedScore = 5 }).IsValid);
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
