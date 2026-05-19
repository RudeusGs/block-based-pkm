namespace Pkm.Api.Contracts.Requests.Workspaces;

public sealed record AddWorkspaceMemberRequest(
    string Email,
    string Role);
