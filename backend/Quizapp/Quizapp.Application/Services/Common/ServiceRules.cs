using Quizapp.Application.DTOs.Common;
using Quizapp.Domain.Exceptions;

namespace Quizapp.Application.Services.Common;

internal static class ServiceRules
{
    private const int FirstPage = 1;
    private const int MaximumPageSize = 100;

    public static void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber < FirstPage)
            throw new ValidationException(nameof(pageNumber), "Page number must be positive.");
        if (pageSize is < 1 or > MaximumPageSize)
            throw new ValidationException(nameof(pageSize), $"Page size must be between 1 and {MaximumPageSize}.");
        if ((long)(pageNumber - FirstPage) * pageSize > int.MaxValue)
            throw new ValidationException(nameof(pageNumber), "Page offset is too large.");
    }

    public static PagedResultDto<TOut> MapPage<TIn, TOut>(PagedResultDto<TIn> page, Func<TIn, TOut> map) => new()
    {
        Items = page.Items.Select(map).ToArray(),
        TotalCount = page.TotalCount,
        PageNumber = page.PageNumber,
        PageSize = page.PageSize
    };

    public static string? Search(string? search) => string.IsNullOrWhiteSpace(search) ? null : search.Trim();
}
