using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Data;

public class DatabaseMigratorTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    [Theory]
    [InlineData("--migrate")]
    [InlineData("--MIGRATE")]
    [InlineData("migrate")]
    [InlineData("MIGRATE")]
    [InlineData("--apply-migrations")]
    public void IsMigrationCommand_returns_true_for_valid_migration_arguments(string argument)
    {
        Assert.True(DatabaseMigrator.IsMigrationCommand([argument]));
        Assert.True(DatabaseMigrator.IsMigrationCommand(["--environment", "Development", argument]));
    }

    [Theory]
    [InlineData("--seed")]
    [InlineData("run")]
    [InlineData("--urls")]
    [InlineData("http://+:8080")]
    public void IsMigrationCommand_returns_false_for_non_migration_arguments(string argument)
    {
        Assert.False(DatabaseMigrator.IsMigrationCommand([argument]));
    }

    [Fact]
    public void IsMigrationCommand_returns_false_for_null_or_empty_arguments()
    {
        Assert.False(DatabaseMigrator.IsMigrationCommand(null));
        Assert.False(DatabaseMigrator.IsMigrationCommand([]));
    }

    [SqlServerFact]
    public async Task MigrateAsync_initializes_empty_schema_before_seeding_and_is_idempotent()
    {
        await using var context = database.CreateContext();
        // Remove the fixture's migrations only from its isolated test database.
        await context.GetService<IMigrator>().MigrateAsync(Migration.InitialDatabase);
        Assert.Empty(await context.Database.GetAppliedMigrationsAsync());
        var missingTable = await Assert.ThrowsAsync<SqlException>(() => context.Roles.AnyAsync());
        Assert.Equal(208, missingTable.Number);

        await DatabaseMigrator.MigrateAsync(context);
        await SampleQuizDataSeeder.SeedAsync(context);

        Assert.True(await context.Roles.AnyAsync());
        var quizCount = await context.Quizzes.CountAsync();
        Assert.True(quizCount > 0);

        // Repeated execution demonstrates idempotency
        await DatabaseMigrator.MigrateAsync(context);
        await SampleQuizDataSeeder.SeedAsync(context);

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(quizCount, await context.Quizzes.CountAsync());
    }
}
