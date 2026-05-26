namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceTrashPagedResultDto(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<WorkspaceTrashItemDto> Items);
