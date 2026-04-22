namespace Pkm.Application.Features.Pages.Models;

public sealed record PagePagedResultDto(
    IReadOnlyList<PageDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);