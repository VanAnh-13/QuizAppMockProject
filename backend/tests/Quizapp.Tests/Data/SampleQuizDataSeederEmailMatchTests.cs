using Microsoft.EntityFrameworkCore;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Data;

public class SampleQuizDataSeederEmailMatchTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private static readonly DateTime ExistingTimestamp = new(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);

    [SqlServerFact]
    public async Task Seeding_reuses_existing_user_holding_the_demo_email_when_username_differs()
    {
        var existingUserId = Guid.NewGuid();

        await using (var context = database.CreateContext())
        {
            context.Users.Add(new User
            {
                Id = existingUserId,
                Username = "hocsinh_demo",
                Email = "demo@quizapp.local",
                Password = "existing-password-hash",
                FullName = "Existing demo learner",
                Status = UserStatus.Active,
                CreateAt = ExistingTimestamp,
                UpdateAt = ExistingTimestamp
            });
            await context.SaveChangesAsync();

            await SampleQuizDataSeeder.SeedAsync(context);
        }

        await using var verification = database.CreateContext();

        var userByEmail = await verification.Users.SingleAsync(user => user.Email == "demo@quizapp.local");
        Assert.Equal(existingUserId, userByEmail.Id);
        Assert.Equal("hocsinh_demo", userByEmail.Username);
        Assert.Equal("existing-password-hash", userByEmail.Password);
        Assert.Equal(0, await verification.Users.CountAsync(user => user.Username == "demo_user"));

        var userRole = await verification.Roles.SingleAsync(role => role.RoleName == "User");
        Assert.True(await verification.UserRoles.AnyAsync(assignment =>
            assignment.UserId == existingUserId && assignment.RoleId == userRole.Id));
    }
}
