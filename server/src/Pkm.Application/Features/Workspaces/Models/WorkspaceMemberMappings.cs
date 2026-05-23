using Pkm.Domain.Users;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public static class WorkspaceMemberMappings
{
    public static WorkspaceMemberDto ToDto(
        this WorkspaceMemberReadModel model,
        Guid currentUserId)
        => new(
            WorkspaceId: model.WorkspaceId,
            UserId: model.UserId,
            UserName: model.UserName,
            Email: model.Email,
            FullName: model.FullName,
            AvatarUrl: model.AvatarUrl,
            UserStatus: model.UserStatus,
            Role: model.Role,
            IsOwner: model.IsOwner,
            IsCurrentUser: model.UserId == currentUserId,
            JoinedAt: model.CreatedDate,
            UpdatedDate: model.UpdatedDate);

    public static WorkspaceMemberDto ToDto(
        this WorkspaceMember member,
        User user,
        Guid currentUserId)
        => new(
            WorkspaceId: member.WorkspaceId,
            UserId: member.UserId,
            UserName: user.UserName,
            Email: user.Email,
            FullName: user.FullName,
            AvatarUrl: user.AvatarUrl,
            UserStatus: user.Status,
            Role: member.Role,
            IsOwner: member.IsOwner(),
            IsCurrentUser: member.UserId == currentUserId,
            JoinedAt: member.CreatedDate,
            UpdatedDate: member.UpdatedDate);
}
