using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Api.Contracts.Responses.Workspaces;

public static class WorkspaceResponseMappings
{
    public static WorkspaceDashboardResponse ToResponse(this WorkspaceDashboardDto dto)
        => new(
            dto.Workspace.ToResponse(),
            dto.Stats.ToResponse(),
            dto.RecentPages.Select(x => x.ToDashboardResponse()).ToArray(),
            dto.MyOpenTasks.Select(x => x.ToDashboardResponse()).ToArray(),
            dto.LatestActivities.Select(x => x.ToDashboardResponse()).ToArray(),
            dto.Members.Select(x => x.ToResponse()).ToArray());

    private static WorkspaceDashboardStatsResponse ToResponse(this WorkspaceDashboardStatsDto dto)
        => new(
            dto.PageCount,
            dto.OpenTaskCount,
            dto.MyOpenTaskCount,
            dto.MemberCount);

    private static WorkspaceDashboardPageResponse ToDashboardResponse(this Pkm.Application.Features.Pages.Models.PageDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.ParentPageId,
            dto.Title,
            dto.Icon,
            dto.CoverImage,
            dto.CurrentRevision,
            dto.CreatedBy,
            dto.LastModifiedBy,
            dto.CreatedDate,
            dto.UpdatedDate);

    private static WorkspaceDashboardTaskResponse ToDashboardResponse(this Pkm.Application.Features.Tasks.Models.WorkTaskDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.PageId,
            dto.Title,
            dto.Description,
            dto.Status.ToString(),
            dto.Priority.ToString(),
            dto.DueDate,
            dto.CreatedById,
            dto.LastModifiedById,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.Assignees.Select(x => new WorkspaceDashboardTaskAssigneeResponse(x.UserId)).ToArray());

    private static WorkspaceDashboardActivityResponse ToDashboardResponse(this Pkm.Application.Features.Activity.Models.ActivityLogDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.UserId,
            dto.UserName,
            dto.UserFullName,
            dto.UserAvatarUrl,
            dto.Action.ToString(),
            dto.EntityType.ToString(),
            dto.EntityId,
            dto.Description,
            dto.MetadataJson,
            dto.OccurredAt,
            dto.CreatedDate);

    public static WorkspaceResponse ToResponse(this WorkspaceDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.AvatarUrl,
            dto.Visibility.ToString(),
            dto.OwnerId,
            dto.LastModifiedBy,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.CurrentUserRole?.ToString(),
            dto.CanRead,
            dto.CanWrite,
            dto.CanManageMembers);

    public static WorkspaceListItemResponse ToResponse(this WorkspaceListItemDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.AvatarUrl,
            dto.Visibility.ToString(),
            dto.OwnerId,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.CurrentUserRole.ToString());

    public static WorkspaceTrashItemResponse ToResponse(this WorkspaceTrashItemDto dto)
        => new(
            dto.Id,
            dto.Name,
            dto.Description,
            dto.AvatarUrl,
            dto.Visibility.ToString(),
            dto.OwnerId,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.TrashedAt,
            dto.CurrentUserRole.ToString());

    public static WorkspaceMemberResponse ToResponse(this WorkspaceMemberDto dto)
        => new(
            dto.WorkspaceId,
            dto.UserId,
            dto.UserName,
            dto.Email,
            dto.FullName,
            dto.AvatarUrl,
            dto.UserStatus.ToString(),
            dto.Role.ToString(),
            dto.IsOwner,
            dto.IsCurrentUser,
            dto.JoinedAt,
            dto.UpdatedDate);

    public static WorkspaceMemberPagedResultResponse ToResponse(this WorkspaceMemberPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);

    public static WorkspaceInvitationResponse ToResponse(this WorkspaceInvitationDto dto)
        => new(
            dto.Id,
            dto.WorkspaceId,
            dto.Email,
            dto.Role.ToString(),
            dto.InvitedByUserId,
            dto.ExpiresAtUtc,
            dto.AcceptedAtUtc,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static WorkspacePagedResultResponse ToResponse(this WorkspacePagedResultDto dto)
        => new(
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.Items.Select(x => x.ToResponse()).ToArray());

    public static WorkspaceTrashPagedResultResponse ToResponse(this WorkspaceTrashPagedResultDto dto)
        => new(
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.Items.Select(x => x.ToResponse()).ToArray());
}
