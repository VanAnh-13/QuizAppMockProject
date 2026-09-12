using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quizapp.Infrastructure.Persistence;
using Quizapp.Infrastructure.Persistence.Repositories;

namespace Quizapp.Tests.Data;

public class EfQuizRepositoryQueryTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Get_active_list_orders_by_title_then_id_before_pagination(int pageNumber)
    {
        await using var context = new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseSqlServer()
            .AddInterceptors(new SuppressConnectionOpening(), new CaptureQuery())
            .Options);

        var captured = await Assert.ThrowsAsync<QueryCapturedException>(() =>
            new EfQuizRepository(context).GetActiveListAsync(pageNumber, 20, null, CancellationToken.None));

        Assert.Matches(@"ORDER BY (?<quiz>\[[^\]]+\])\.\[Title\], \k<quiz>\.\[Id\]\s+OFFSET ", captured.Sql);
    }

    [Fact]
    public async Task Get_active_list_requests_question_active_state_in_the_sql_query()
    {
        await using var context = new QuizAppDbContext(new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseSqlServer()
            .AddInterceptors(new SuppressConnectionOpening(), new CaptureQuery())
            .Options);

        var captured = await Assert.ThrowsAsync<QueryCapturedException>(() =>
            new EfQuizRepository(context).GetActiveListAsync(1, 20, null, CancellationToken.None));

        var questionJoin = Regex.Match(captured.Sql, @"JOIN \[Questions\] AS (?<alias>\[[^\]]+\])");
        Assert.True(questionJoin.Success, captured.Sql);
        Assert.Contains($"{questionJoin.Groups["alias"].Value}.[IsActive]", captured.Sql);
    }

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
