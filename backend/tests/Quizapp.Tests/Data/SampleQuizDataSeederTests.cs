using Microsoft.EntityFrameworkCore;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Tests.Data;

public class SampleQuizDataSeederTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [SqlServerFact]
    public async Task Seeded_quizzes_are_active_catalog_entries_and_seeding_is_idempotent()
    {
        await using (var context = database.CreateContext())
        {
            await SampleQuizDataSeeder.SeedAsync(context);
            await SampleQuizDataSeeder.SeedAsync(context);
        }

        await using var verificationContext = database.CreateContext();
        var catalog = await new EfQuizRepository(verificationContext)
            .GetActiveListAsync(1, 100, null, CancellationToken.None);

        Assert.Equal(30, catalog.TotalCount);
        Assert.All(catalog.Items, quiz => Assert.NotEmpty(quiz.QuizQuestions));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("C#", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("SQL", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("Angular", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("TypeScript", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("API", StringComparison.Ordinal));
    }

    [SqlServerFact]
    public async Task Seeding_creates_demo_admin_with_admin_role()
    {
        await using (var context = database.CreateContext())
        {
            await SampleQuizDataSeeder.SeedAsync(context);
            await SampleQuizDataSeeder.SeedAsync(context);
        }

        await using var verification = database.CreateContext();
        var admin = await verification.Users.SingleAsync(user => user.Username == "demo_admin");
        var adminRole = await verification.Roles.SingleAsync(role => role.RoleName == "Admin");

        Assert.Equal("admin@quizapp.local", admin.Email);
        Assert.True(await verification.UserRoles.AnyAsync(assignment =>
            assignment.UserId == admin.Id && assignment.RoleId == adminRole.Id));
    }
}