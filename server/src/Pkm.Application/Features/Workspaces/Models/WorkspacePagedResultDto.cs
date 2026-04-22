namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspacePagedResultDto(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<WorkspaceListItemDto> Items);