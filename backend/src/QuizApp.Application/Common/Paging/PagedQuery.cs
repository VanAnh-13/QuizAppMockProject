namespace QuizApp.Application.Common.Paging;

/// <summary>
/// Shared paging/search/sort contract. Values are clamped by services to
/// configured bounds (see Paging:DefaultPageSize / Paging:MaxPageSize).
/// </summary>
public class PagedQuery
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Search { get; set; }

    public string? SortBy { get; set; }

    /// <summary>"asc" or "desc".</summary>
    public string? SortDir { get; set; }
}
