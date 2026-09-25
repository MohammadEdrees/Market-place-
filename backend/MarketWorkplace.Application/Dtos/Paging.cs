namespace MarketWorkplace.Application.Dtos;

/// <summary>Envelope returned by paginated list endpoints.</summary>
/// <typeparam name="T">Row type.</typeparam>
/// <param name="Items">Rows for the requested page.</param>
/// <param name="Total">Total rows matching the filters, across all pages.</param>
/// <param name="Page">1-based page number actually returned.</param>
/// <param name="PageSize">Rows per page actually applied.</param>
public record PagedResponse<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>Shared normalisation for the <c>page</c> / <c>pageSize</c> query parameters.</summary>
public static class Paging
{
    /// <summary>Upper bound accepted for <c>pageSize</c>.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Rows per page used when the caller sends no (or an invalid) page size.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Forces <paramref name="page"/> to a 1-based value.</summary>
    public static int NormalizePage(int page) => page < 1 ? 1 : page;

    /// <summary>Clamps <paramref name="pageSize"/> into [1..100]; non-positive input falls back to <see cref="DefaultPageSize"/>.</summary>
    public static int NormalizePageSize(int pageSize) =>
        pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
