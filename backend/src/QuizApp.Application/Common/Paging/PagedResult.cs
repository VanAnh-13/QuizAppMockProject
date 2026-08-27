namespace QuizApp.Application.Common.Paging;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages { get; set; }

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalItems, int page, int pageSize)
    {
        return new PagedResult<T>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize,
            TotalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }
}
