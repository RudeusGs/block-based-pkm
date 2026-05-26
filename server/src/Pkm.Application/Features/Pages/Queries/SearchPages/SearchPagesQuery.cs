using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.SearchPages;

public sealed record SearchPagesQuery(
    Guid WorkspaceId,
    string Keyword,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagePagedResultDto>;
