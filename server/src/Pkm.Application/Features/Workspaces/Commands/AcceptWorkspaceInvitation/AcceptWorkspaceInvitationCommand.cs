using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Commands.AcceptWorkspaceInvitation;

public sealed record AcceptWorkspaceInvitationCommand(string Token) : ICommand<WorkspaceMemberDto>;
