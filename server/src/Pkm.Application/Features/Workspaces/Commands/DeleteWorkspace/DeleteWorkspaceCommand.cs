using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Workspaces.Commands.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(Guid WorkspaceId) : ICommand;
