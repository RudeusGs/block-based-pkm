using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.ChangeWorkspaceMemberRole;

public sealed record ChangeWorkspaceMemberRoleCommand(
    Guid WorkspaceId,
    Guid UserId,
    WorkspaceRole Role) : ICommand<WorkspaceMemberDto>;
