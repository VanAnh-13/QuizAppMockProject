using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Services.UserManager;

namespace Quizapp.Tests.Application.Services;

public class UserServiceTests
{
    [Fact]
    public async Task Administrator_can_create_update_and_deactivate_a_user()
    {
        using var context = new ServiceTestContext();
        var service = context.Get<IUserService>();

        var created = await service.CreateAsync(new CreateUserDto
        {
            Username = "learner", Email = "learner@example.com", Password = "test-password-123",
            ConfirmPassword = "test-password-123", Profile = new UserProfileDto { FullName = "Learner" },
            IsActive = true
        });

        await service.UpdateAsync(created.Id, new UpdateUserDto
        {
            Username = "learner", Email = "new@example.com", Profile = new UserProfileDto { FullName = "New name" },
            IsActive = true
        });

        await service.SetActiveAsync(created.Id, false);
        var result = await service.GetByIdAsync(created.Id);

        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("New name", result.FullName);
        Assert.False(result.IsActive);
    }
}
