using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed record AddWorkspaceMemberCommand(
    Guid WorkspaceId,
    string Email,
    WorkspaceRole Role) : ICommand<WorkspaceInvitationDto>;
