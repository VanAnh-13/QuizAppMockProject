using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Quizapp.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static bool IsMigrationCommand(string[]? args)
    {
        if (args is null || args.Length == 0)
            return false;

        return args.Any(arg => string.Equals(arg, "--migrate", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(arg, "migrate", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(arg, "--apply-migrations", StringComparison.OrdinalIgnoreCase));
    }

    public static async Task MigrateAsync(
        QuizAppDbContext db,
        ILogger? logger = null,
        int maxRetries = 5,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        var delay = retryDelay ?? TimeSpan.FromSeconds(2);
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger?.LogInformation("Applying EF Core database migrations (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                await db.Database.MigrateAsync(cancellationToken);
                logger?.LogInformation("EF Core database migrations applied successfully.");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                logger?.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying in {Delay}s...", attempt, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
