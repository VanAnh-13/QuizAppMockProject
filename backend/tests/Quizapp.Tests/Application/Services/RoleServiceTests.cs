using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Application.Services.RoleManager;
using Quizapp.Domain.Exceptions;
using ValidationException = FluentValidation.ValidationException;

namespace Quizapp.Tests.Application.Services;

public class RoleServiceTests
{
    [Fact]
    public async Task Creation_preserves_input_and_creates_independent_unassigned_roles()
    {
        using var context = new ServiceTestContext();
        var service = context.Get<IRoleService>();
        var request = new CreateRoleDto
        {
            RoleName = " Reviewer ", Description = "Reviews questions", IsActive = false
        };

        var first = await service.CreateAsync(request);
        var second = await service.CreateAsync(new CreateRoleDto { RoleName = "Editor" });

        Assert.Equal(" Reviewer ", request.RoleName);
        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal("Reviewer", first.RoleName);
        Assert.Equal("Reviews questions", first.Description);
        Assert.False(first.IsActive);
        var saved = await context.Roles.GetByIdAsync(first.Id, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Empty(saved.Users);
        Assert.Empty(saved.UserRoles);
    }

    [Fact]
    public async Task Null_and_invalid_creation_requests_do_not_persist_roles()
    {
        using var context = new ServiceTestContext();
        var service = context.Get<IRoleService>();
        var originalCount = context.Roles.Rows.Count;

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(new CreateRoleDto { RoleName = " " }));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(CreateRoleDto.RoleName));
        Assert.Equal(originalCount, context.Roles.Rows.Count);
    }

    [Fact]
    public async Task Duplicate_trimmed_role_names_are_rejected_without_persisting_another_role()
    {
        using var context = new ServiceTestContext();
        var service = context.Get<IRoleService>();
        await service.CreateAsync(new CreateRoleDto { RoleName = "Reviewer" });
        var originalCount = context.Roles.Rows.Count;

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(new CreateRoleDto { RoleName = " Reviewer " }));

        Assert.Equal(originalCount, context.Roles.Rows.Count);
    }

    [Fact]
    public async Task Administrator_can_create_and_retrieve_a_custom_role()
    {
        using var context = new ServiceTestContext();
        var service = context.Get<IRoleService>();

        var created = await service.CreateAsync(new CreateRoleDto
        {
            RoleName = "Reviewer", Description = "Reviews questions", IsActive = true
        });

        var found = await service.GetByIdAsync(created.Id);

        Assert.NotEqual(Guid.Empty, found.Id);
        Assert.Equal("Reviewer", found.RoleName);
        Assert.Equal("Reviews questions", found.Description);
        Assert.True(found.IsActive);
    }
}
