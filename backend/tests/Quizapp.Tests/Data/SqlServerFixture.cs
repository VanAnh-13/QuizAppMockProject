using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Quizapp.Infrastructure.Persistence;

namespace Quizapp.Tests.Data;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerFixture.ConnectionVariable)))
            Skip = $"Set {SqlServerFixture.ConnectionVariable} to run against an isolated SQL Server test database.";
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerFixture.ConnectionVariable)))
            Skip = $"Set {SqlServerFixture.ConnectionVariable} to run against an isolated SQL Server test database.";
    }
}

// xUnit creates this fixture for test classes implementing IClassFixture<SqlServerFixture>.
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string ConnectionVariable = "QUIZAPP_TEST_SQLSERVER_CONNECTION_STRING";
    private const string DatabasePrefix = "QuizappRelationTests_";
    private readonly string? _connectionString;

    public SqlServerFixture()
    {
        var configuredConnection = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(configuredConnection))
            return;

        // Never create, clear, or delete the database named in the supplied connection string.
        var builder = new SqlConnectionStringBuilder(configuredConnection)
        {
            InitialCatalog = DatabasePrefix + Guid.NewGuid().ToString("N"),
            Pooling = false
        };
        _connectionString = builder.ConnectionString;
    }

    public QuizAppDbContext CreateContext()
    {
        if (_connectionString is null)
            throw new InvalidOperationException($"Missing {ConnectionVariable}.");

        return new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseSqlServer(_connectionString).Options);
    }

    public async Task InitializeAsync()
    {
        if (_connectionString is null)
            return;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_connectionString is null)
            return;

        if (!new SqlConnectionStringBuilder(_connectionString).InitialCatalog.StartsWith(DatabasePrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete a database outside the test namespace.");

        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
