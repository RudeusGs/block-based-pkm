namespace Pkm.Application.Features.Tasks.Models;

public sealed record WorkTaskPagedResultDto(
    IReadOnlyList<WorkTaskDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);