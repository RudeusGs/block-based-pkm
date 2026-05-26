using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.GetWorkspaceDashboard;

public sealed record GetWorkspaceDashboardQuery(Guid WorkspaceId) : IQuery<WorkspaceDashboardDto>;
