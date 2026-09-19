using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Tests.Data;

public class EfUserRepositoryQueryTests
{
    [Fact]
    public async Task Get_list_requests_user_roles_in_the_sql_query()
    {
        await using var context = new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseSqlServer()
            .AddInterceptors(new SuppressConnectionOpening(), new CaptureQuery())
            .Options);

        var captured = await Assert.ThrowsAsync<QueryCapturedException>(() =>
            new EfUserRepository(context).GetListAsync(1, 20, null, CancellationToken.None));

        Assert.Contains("[Roles]", captured.Sql);
    }

    private sealed class SuppressConnectionOpening : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class CaptureQuery : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("SELECT COUNT(*)", StringComparison.Ordinal))
            {
                using var count = new DataTable();
                count.Columns.Add("Count", typeof(int));
                count.Rows.Add(1);
                return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(count.CreateDataReader()));
            }

            throw new QueryCapturedException(command.CommandText);
        }
    }

    private sealed class QueryCapturedException(string sql) : Exception
    {
        public string Sql { get; } = sql;
    }
}
