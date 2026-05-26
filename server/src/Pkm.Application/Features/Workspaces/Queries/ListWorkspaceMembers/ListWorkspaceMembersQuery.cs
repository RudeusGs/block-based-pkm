using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;

public sealed record ListWorkspaceMembersQuery(
    Guid WorkspaceId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<WorkspaceMemberPagedResultDto>;
