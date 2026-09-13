using Microsoft.EntityFrameworkCore;
using Quizapp.Domain.Entities;
using Quizapp.Domain.Enums;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Data;

public class SampleQuizDataSeederExistingDataTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private static readonly DateTime ExistingTimestamp = new(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc);

    [SqlServerFact]
    public async Task Seeding_reuses_existing_roles_and_demo_user_matched_by_natural_keys()
    {
        var adminRoleId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();
        var demoUserId = Guid.NewGuid();

        await using (var context = database.CreateContext())
        {
            context.Roles.AddRange(
                new Role
                {
                    Id = adminRoleId,
                    RoleName = "Admin",
                    Description = "Existing admin role",
                    IsActive = true
                },
                new Role
                {
                    Id = userRoleId,
                    RoleName = "User",
                    Description = "Existing user role",
                    IsActive = true
                });
            context.Users.Add(new User
            {
                Id = demoUserId,
                Username = "demo_user",
                Email = "demo@quizapp.local",
                Password = "existing-password-hash",
                FullName = "Existing demo learner",
                Status = UserStatus.Active,
                CreateAt = ExistingTimestamp,
                UpdateAt = ExistingTimestamp
            });
            await context.SaveChangesAsync();

            await SampleQuizDataSeeder.SeedAsync(context);
            await SampleQuizDataSeeder.SeedAsync(context);
        }

        await using var verification = database.CreateContext();

        var adminRole = await verification.Roles.SingleAsync(role => role.RoleName == "Admin");
        var userRole = await verification.Roles.SingleAsync(role => role.RoleName == "User");
        Assert.Equal(adminRoleId, adminRole.Id);
        Assert.Equal(userRoleId, userRole.Id);
        Assert.Equal("Existing admin role", adminRole.Description);

        var demoUser = await verification.Users.SingleAsync(user => user.Username == "demo_user");
        Assert.Equal(demoUserId, demoUser.Id);
        Assert.Equal("existing-password-hash", demoUser.Password);

        var assignments = await verification.UserRoles.ToListAsync();
        var demoAssignment = Assert.Single(assignments, assignment => assignment.UserId == demoUserId);
        Assert.Equal(userRoleId, demoAssignment.RoleId);
        Assert.DoesNotContain(assignments,
            assignment => assignment.UserId == demoUserId && assignment.RoleId == adminRoleId);

        var seededQuizCount = await verification.Quizzes
            .CountAsync(quiz => SampleQuizDataSeeder.SampleQuizIds.Contains(quiz.Id));
        Assert.Equal(SampleQuizDataSeeder.SampleQuizIds.Length, seededQuizCount);
    }
}
