using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record AcceptWorkspaceShareCommand(Guid MessageId)
    : ICommand<WorkspaceDto>;
