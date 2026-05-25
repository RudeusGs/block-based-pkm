using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;

public sealed record RemoveWorkspaceMemberCommand(
    Guid WorkspaceId,
    Guid UserId) : ICommand;
