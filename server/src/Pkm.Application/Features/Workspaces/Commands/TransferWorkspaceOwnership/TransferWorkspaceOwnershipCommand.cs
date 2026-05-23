namespace Pkm.Application.Features.Workspaces.Commands.TransferWorkspaceOwnership;

public sealed record TransferWorkspaceOwnershipCommand(Guid WorkspaceId, Guid NewOwnerUserId);
