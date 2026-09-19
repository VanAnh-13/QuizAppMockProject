using Quizapp.Application.DTOs.Authentication;
using Quizapp.Application.DTOs.UserManager;
using Quizapp.Application.Services.Authentication;
using Quizapp.Application.Services.Common;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Tests.Application.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task Registration_creates_a_regular_account_that_can_sign_in()
    {
        using var context = new ServiceTestContext();
        context.CurrentUser.UserId = null;
        var service = context.Get<IAuthService>();

        var user = await service.RegisterAsync(new RegisterDto
        {
            Username = "student", Email = "student@example.com", Password = "test-password-123",
            ConfirmPassword = "test-password-123", Profile = new UserProfileDto { FullName = "Student" }
        });

        var login = await service.LoginAsync(new LoginDto { Username = "student", Password = "test-password-123" });

        Assert.Equal(user.Id, login.UserDto.Id);
        Assert.Empty(user.Roles);
        Assert.True(user.IsActive);
        Assert.NotEmpty(login.Token);
        Assert.True(login.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Current_user_returns_the_signed_in_profile_with_roles()
    {
        using var context = new ServiceTestContext();
        var expected = context.Users.Rows.Single(user => user.Id == context.CurrentUser.UserId);
        var service = context.Get<IAuthService>();

        var profile = await service.GetCurrentUserAsync();

        Assert.Equal(expected.Id, profile.Id);
        Assert.Equal(expected.Username, profile.Username);
        Assert.Equal(expected.Email, profile.Email);
        Assert.Equal(ServiceAuthorization.AdministratorRole, Assert.Single(profile.Roles).RoleName);
    }

    [Fact]
    public async Task Current_user_requires_an_authenticated_caller()
    {
        using var context = new ServiceTestContext();
        context.CurrentUser.UserId = null;
        var service = context.Get<IAuthService>();

        await Assert.ThrowsAsync<AuthenticationException>(() => service.GetCurrentUserAsync());
    }
}
