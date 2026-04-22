using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Models;

public sealed record WorkspaceAccessReadModel(
    Guid WorkspaceId,
    Guid OwnerId,
    WorkspaceRole? Role);