using Microsoft.EntityFrameworkCore;
using QuizApp.Infrastructure;
using QuizApp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();

var app = builder.Build();

// Apply migrations and seed data automatically, tolerating a slow SQL Server boot.
await ApplyDatabaseAsync(app);

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { name = "QuizApp API", status = "running" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

static async Task ApplyDatabaseAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
    var autoMigrate = app.Configuration.GetValue("Database:AutoMigrate", true);
    if (!autoMigrate)
    {
        return;
    }

    const int maxAttempts = 12;
    const int delaySeconds = 5;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();
            await DbSeeder.SeedAsync(scope.ServiceProvider);
            logger.LogInformation("Database migrated and seeded successfully.");
            return;
        }
        catch (Exception ex)
        {
            if (attempt == maxAttempts)
            {
                logger.LogError(ex, "Database not ready after {Attempts} attempts - continuing without migration.", maxAttempts);
                return;
            }

            logger.LogWarning("Database not ready (attempt {Attempt}/{Attempts}), retrying in {Delay}s...",
                attempt, maxAttempts, delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
}
