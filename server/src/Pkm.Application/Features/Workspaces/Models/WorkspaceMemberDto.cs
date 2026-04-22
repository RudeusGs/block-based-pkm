using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceMemberDto(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role,
    bool IsOwner,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);