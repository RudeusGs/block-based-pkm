using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed record AddWorkspaceMemberCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role);