using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Commands.RestoreWorkspace;

public sealed record RestoreWorkspaceCommand(Guid WorkspaceId) : ICommand<WorkspaceDto>;
