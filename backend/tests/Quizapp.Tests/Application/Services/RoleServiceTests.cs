using Quizapp.Application.DTOs.RoleManager;
using Quizapp.Application.Services.RoleManager;

namespace Quizapp.Tests.Application.Services;

public class RoleServiceTests
{
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
