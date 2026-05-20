using Pkm.Domain.Users;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceMemberReadModel(
    Guid WorkspaceId,
    Guid UserId,
    string UserName,
    string Email,
    string FullName,
    string? AvatarUrl,
    UserStatus UserStatus,
    WorkspaceRole Role,
    bool IsOwner,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);