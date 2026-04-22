using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceMemberReadModel(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);