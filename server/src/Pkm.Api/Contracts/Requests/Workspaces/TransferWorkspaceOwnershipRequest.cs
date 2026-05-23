namespace Pkm.Api.Contracts.Requests.Workspaces;

public sealed record TransferWorkspaceOwnershipRequest(
    Guid NewOwnerUserId);
