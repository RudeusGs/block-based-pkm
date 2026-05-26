using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.ListTrashedWorkspaces;

public sealed record ListTrashedWorkspacesQuery(
    int PageNumber = 1,
    int PageSize = 20) : IQuery<WorkspaceTrashPagedResultDto>;
