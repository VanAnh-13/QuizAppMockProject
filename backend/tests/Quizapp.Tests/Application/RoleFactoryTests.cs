using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Quizapp.Application;
using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Application.Factories.RoleManager;

namespace Quizapp.Tests.Application;

public class RoleFactoryTests
{
    [Fact]
    public void Factory_creates_custom_roles_with_independent_ids_and_preserves_input()
    {
        using var provider = new ServiceCollection().AddApplication()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IRoleFactory>();

        var request = new CreateRoleDto.Builder().WithRoleName("Question Reviewer")
            .WithDescription("Reviews question content")
            .WithIsActive(false)
            .Build();

        var first = factory.Create(request);
        var second = factory.Create(request);

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Question Reviewer", first.RoleName);
        Assert.Equal("Reviews question content", first.Description);
        Assert.False(first.IsActive);
        Assert.Empty(first.Users);
        Assert.Empty(first.UserRoles);
    }

    [Fact]
    public void Factory_rejects_null_and_invalid_role_requests()
    {
        using var provider = new ServiceCollection().AddApplication()
            .BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IRoleFactory>();

        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));

        var exception = Assert.Throws<ValidationException>(() =>
            factory.Create(new CreateRoleDto.Builder().WithRoleName(" ")
                .Build()));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateRoleDto.RoleName));
    }
}
