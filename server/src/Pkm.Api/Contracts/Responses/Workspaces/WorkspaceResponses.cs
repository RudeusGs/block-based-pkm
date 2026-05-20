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
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    string UserStatus,
    string Role,
    bool IsOwner,
    bool IsCurrentUser,
    DateTimeOffset JoinedAt,
    DateTimeOffset? UpdatedDate);

public sealed record WorkspaceInvitationResponse(
    Guid Id,
    Guid WorkspaceId,
    string Email,
    string Role,
    Guid InvitedByUserId,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record WorkspacePagedResultResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<WorkspaceListItemResponse> Items);