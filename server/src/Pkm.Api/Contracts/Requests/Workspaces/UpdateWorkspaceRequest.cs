namespace Pkm.Api.Contracts.Requests.Workspaces;

public sealed record UpdateWorkspaceRequest(
    string Name,
    string? Description);