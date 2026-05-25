using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Commands.JoinPublicWorkspaceAsViewer;

public sealed record JoinPublicWorkspaceAsViewerCommand(Guid WorkspaceId) : ICommand<WorkspaceDto>;
