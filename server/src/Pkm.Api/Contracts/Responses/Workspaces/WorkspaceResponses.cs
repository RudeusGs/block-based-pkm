namespace Pkm.Api.Contracts.Responses.Workspaces;

public sealed record WorkspaceResponse(
    Guid Id,
    string Name,
    string? Description,
    string? AvatarUrl,
    string Visibility,
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
    string? AvatarUrl,
    string Visibility,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    string CurrentUserRole);

public sealed record WorkspaceTrashItemResponse(
    Guid Id,
    string Name,
    string? Description,
    string? AvatarUrl,
    string Visibility,
    Guid OwnerId,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    DateTimeOffset? TrashedAt,
    string CurrentUserRole);


public sealed record WorkspaceDashboardResponse(
    WorkspaceResponse Workspace,
    WorkspaceDashboardStatsResponse Stats,
    IReadOnlyList<WorkspaceDashboardPageResponse> RecentPages,
    IReadOnlyList<WorkspaceDashboardTaskResponse> MyOpenTasks,
    IReadOnlyList<WorkspaceDashboardActivityResponse> LatestActivities,
    IReadOnlyList<WorkspaceMemberResponse> Members);

public sealed record WorkspaceDashboardStatsResponse(
    int PageCount,
    int OpenTaskCount,
    int MyOpenTaskCount,
    int MemberCount);

public sealed record WorkspaceDashboardPageResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? ParentPageId,
    string Title,
    string? Icon,
    string? CoverImage,
    long CurrentRevision,
    Guid CreatedBy,
    Guid? LastModifiedBy,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record WorkspaceDashboardTaskAssigneeResponse(
    Guid UserId);

public sealed record WorkspaceDashboardTaskResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid? PageId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTimeOffset? DueDate,
    Guid CreatedById,
    Guid? LastModifiedById,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    IReadOnlyList<WorkspaceDashboardTaskAssigneeResponse> Assignees);

public sealed record WorkspaceDashboardActivityResponse(
    Guid Id,
    Guid WorkspaceId,
    Guid UserId,
    string? UserName,
    string? UserFullName,
    string? UserAvatarUrl,
    string Action,
    string EntityType,
    Guid EntityId,
    string? Description,
    string? MetadataJson,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedDate);

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

public sealed record WorkspaceMemberPagedResultResponse(
    IReadOnlyList<WorkspaceMemberResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

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

public sealed record WorkspaceTrashPagedResultResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    IReadOnlyList<WorkspaceTrashItemResponse> Items);
