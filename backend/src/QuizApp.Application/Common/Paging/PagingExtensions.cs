using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace QuizApp.Application.Common.Paging;

public static class PagingExtensions
{
    /// <summary>
    /// Applies server-side search/sort/paging in SQL. Page is clamped to >= 1,
    /// pageSize is clamped to [1, maxPageSize]. Sorting uses an explicit
    /// sortable-column map; unknown keys fall back to <paramref name="defaultSort"/>.
    /// </summary>
    public static async Task<PagedResult<TDest>> ToPagedResultAsync<TEntity, TDest>(
        this IQueryable<TEntity> query,
        PagedQuery paged,
        IReadOnlyDictionary<string, Expression<Func<TEntity, object?>>> sortableColumns,
        string defaultSort,
        Func<TEntity, TDest> map,
        int defaultPageSize = 10,
        int maxPageSize = 100,
        CancellationToken ct = default)
    {
        var page = Math.Max(1, paged.Page);
        var pageSize = paged.PageSize <= 0 ? defaultPageSize : Math.Min(paged.PageSize, maxPageSize);

        var sortKey = !string.IsNullOrWhiteSpace(paged.SortBy) && sortableColumns.ContainsKey(paged.SortBy.ToLowerInvariant())
            ? paged.SortBy.ToLowerInvariant()
            : defaultSort;

        var selector = sortableColumns[sortKey];
        var sorted = string.Equals(paged.SortDir, "desc", StringComparison.OrdinalIgnoreCase)
            ? query.OrderByDescending(selector)
            : query.OrderBy(selector);

        var totalItems = await query.CountAsync(ct);
        var items = await sorted.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return PagedResult<TDest>.Create(items.Select(map).ToList(), totalItems, page, pageSize);
    }
}
