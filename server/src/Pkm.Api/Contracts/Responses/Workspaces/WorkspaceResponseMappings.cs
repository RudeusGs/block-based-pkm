using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Api.Contracts.Responses.Workspaces;

public static class WorkspaceResponseMappings
{
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
}
