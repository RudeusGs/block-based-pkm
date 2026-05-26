namespace Pkm.Application.Common.Pagination;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int NormalizedPageNumber => PageNumber <= 0 ? DefaultPageNumber : PageNumber;
    public int NormalizedPageSize => PageSize <= 0 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
    public int Skip => (NormalizedPageNumber - 1) * NormalizedPageSize;

    public static PageRequest Normalize(
        int pageNumber,
        int pageSize,
        int defaultPageSize = DefaultPageSize,
        int maxPageSize = MaxPageSize)
        => new(
            NormalizePageNumber(pageNumber),
            NormalizePageSize(pageSize, defaultPageSize, maxPageSize));

    public static int NormalizePageNumber(int pageNumber)
        => pageNumber <= 0 ? DefaultPageNumber : pageNumber;

    public static int NormalizePageSize(
        int pageSize,
        int defaultPageSize = DefaultPageSize,
        int maxPageSize = MaxPageSize)
        => pageSize <= 0 ? defaultPageSize : Math.Min(pageSize, maxPageSize);

    public static int CalculateTotalPages(int totalCount, int pageSize)
        => totalCount <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
}
