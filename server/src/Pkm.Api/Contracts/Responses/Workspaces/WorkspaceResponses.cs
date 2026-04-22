namespace Pkm.Api.Contracts.Responses.Workspaces;

public sealed record WorkspaceResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    string? CurrentUserRole,
    bool CanRead,
    bool CanWrite,
    bool CanManageMembers);

public sealed record WorkspaceListItemResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    string CurrentUserRole);

public sealed record WorkspaceMemberResponse(
    Guid WorkspaceId,
    Guid UserId,
    string Role,
    bool IsOwner,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record WorkspacePagedResultResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<WorkspaceListItemResponse> Items);