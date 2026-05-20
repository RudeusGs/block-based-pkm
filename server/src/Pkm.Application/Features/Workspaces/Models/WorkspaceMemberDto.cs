using Pkm.Domain.Users;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceMemberDto(
    Guid WorkspaceId,
    Guid UserId,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    UserStatus UserStatus,
    WorkspaceRole Role,
    bool IsOwner,
    bool IsCurrentUser,
    DateTimeOffset JoinedAt,
    DateTimeOffset? UpdatedDate);