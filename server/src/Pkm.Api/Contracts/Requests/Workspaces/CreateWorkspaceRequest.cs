namespace Pkm.Api.Contracts.Requests.Workspaces;

public sealed record CreateWorkspaceRequest(
    string Name,
    string? Description);