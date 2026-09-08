using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Tests.Data;

public class EfQuizRepositoryQueryTests
{
    [Fact]
    public async Task Get_by_id_requests_answer_choices_in_the_sql_query()
    {
        await using var context = new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseSqlServer()
            .AddInterceptors(new SuppressConnectionOpening(), new CaptureQuery())
            .Options);

        var captured = await Assert.ThrowsAsync<QueryCapturedException>(() =>
            new EfQuizRepository(context).GetByIdAsync(Guid.NewGuid(), CancellationToken.None));

        Assert.Contains("[Answers]", captured.Sql);
        Assert.Contains("[IsCorrect]", captured.Sql);
        Assert.Contains("[Text]", captured.Sql);
    }

    // Exercise SQL generation through the real repository without opening a database.
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
            CancellationToken cancellationToken = default) => throw new QueryCapturedException(command.CommandText);
    }

    private sealed class QueryCapturedException(string sql) : Exception
    {
        public string Sql { get; } = sql;
    }
}
