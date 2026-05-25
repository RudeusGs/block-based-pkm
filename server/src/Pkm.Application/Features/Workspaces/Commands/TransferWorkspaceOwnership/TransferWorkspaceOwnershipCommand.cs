using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Commands.TransferWorkspaceOwnership;

public sealed record TransferWorkspaceOwnershipCommand(
    Guid WorkspaceId,
    Guid NewOwnerUserId) : ICommand<WorkspaceDto>;
