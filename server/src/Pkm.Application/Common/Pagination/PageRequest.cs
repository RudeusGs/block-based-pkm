namespace Pkm.Application.Common.Pagination;

public sealed record PageRequest(int PageNumber = 1, int PageSize = 20)
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int NormalizedPageNumber => PageNumber <= 0 ? DefaultPageNumber : PageNumber;
    public int NormalizedPageSize => PageSize <= 0 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
    public int Skip => (NormalizedPageNumber - 1) * NormalizedPageSize;
}
