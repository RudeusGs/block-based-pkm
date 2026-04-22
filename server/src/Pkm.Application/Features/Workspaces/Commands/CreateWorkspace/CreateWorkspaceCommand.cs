namespace Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed record CreateWorkspaceCommand(
    string Name,
    string? Description);