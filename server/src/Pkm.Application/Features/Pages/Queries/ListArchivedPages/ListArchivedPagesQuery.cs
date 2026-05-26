using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListArchivedPages;

public sealed record ListArchivedPagesQuery(
    Guid WorkspaceId,
    int PageNumber,
    int PageSize) : IQuery<PagePagedResultDto>;
