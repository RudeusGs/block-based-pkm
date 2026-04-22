using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.ChangeWorkspaceMemberRole;

public sealed record ChangeWorkspaceMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role);