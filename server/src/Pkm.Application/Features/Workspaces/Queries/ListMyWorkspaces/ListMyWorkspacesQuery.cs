using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;

public sealed record ListMyWorkspacesQuery(
    int PageNumber = 1,
    int PageSize = 20) : IQuery<WorkspacePagedResultDto>;
