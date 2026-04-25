namespace Pkm.Application.Features.Tasks.Models;

public sealed record TaskCommentPagedResultDto(
    IReadOnlyList<TaskCommentDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);