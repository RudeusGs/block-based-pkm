namespace Pkm.Api.Contracts.Requests.Workspaces;

public sealed record AddWorkspaceMemberRequest(
    Guid UserId,
    string Role);