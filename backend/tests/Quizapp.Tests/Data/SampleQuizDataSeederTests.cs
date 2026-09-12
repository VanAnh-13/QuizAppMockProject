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

        Assert.Equal(10, catalog.TotalCount);
        Assert.All(catalog.Items, quiz => Assert.NotEmpty(quiz.QuizQuestions));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("C#", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("SQL", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("Angular", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("TypeScript", StringComparison.Ordinal));
        Assert.Contains(catalog.Items, quiz => quiz.Title.Contains("API", StringComparison.Ordinal));
    }
}