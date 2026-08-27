using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuizApp.Api.IntegrationTests;

/// <summary>
/// Boots the real API against the SQL Server container from docker-compose.yml,
/// using a dedicated database per test run that is dropped on teardown.
/// </summary>
public class QuizApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"QuizApp_Tests_{Guid.NewGuid():N}";

    public static string SaPassword => Environment.GetEnvironmentVariable("TEST_SA_PASSWORD") ?? "Str0ng_Passw0rd!";

    public string ConnectionString =>
        $"Server=localhost,1433;Database={_databaseName};User Id=sa;Password={SaPassword};TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Key"] = "integration-test-key-0123456789abcdef0123456789",
                ["Jwt:Issuer"] = "QuizApp.Api.Tests",
                ["Jwt:Audience"] = "QuizApp.Web.Tests",
                ["Database:AutoMigrate"] = "false",
                ["Seed:Admin:Password"] = "Admin@12345"
            });
        });
    }

    public async Task InitializeAsync()
    {
        // The entry point's ApplyDatabaseAsync is disabled below; the factory
        // migrates and seeds the isolated test database from the built host,
        // whose configuration is guaranteed to carry the overrides above.
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuizApp.Infrastructure.Persistence.AppDbContext>();
        await context.Database.MigrateAsync();
        await QuizApp.Infrastructure.Persistence.DbSeeder.SeedAsync(scope.ServiceProvider);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        await using var connection = new SqlConnection(
            $"Server=localhost,1433;Database=master;User Id=sa;Password={SaPassword};TrustServerCertificate=True");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"IF DB_ID('{_databaseName}') IS NOT NULL BEGIN " +
                              $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                              $"DROP DATABASE [{_databaseName}]; END";
        await command.ExecuteNonQueryAsync();
    }
}
