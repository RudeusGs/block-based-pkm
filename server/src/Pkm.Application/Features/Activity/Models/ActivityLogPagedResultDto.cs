namespace Pkm.Application.Features.Activity.Models;

public sealed record ActivityLogPagedResultDto(
    IReadOnlyList<ActivityLogDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
