using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(
    Guid WorkspaceId,
    string Name,
    string? Description,
    WorkspaceVisibility? Visibility = null) : ICommand<WorkspaceDto>;
