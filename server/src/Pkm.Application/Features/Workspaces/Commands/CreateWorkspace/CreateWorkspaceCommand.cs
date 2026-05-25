using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(
    string Name,
    string? Description,
    WorkspaceVisibility Visibility = WorkspaceVisibility.Private) : ICommand<WorkspaceDto>;
