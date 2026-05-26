using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListWorkspacePages;

public sealed record ListWorkspacePagesQuery(
    Guid WorkspaceId,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagePagedResultDto>;
