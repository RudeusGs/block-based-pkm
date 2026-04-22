namespace Pkm.Application.Features.Pages.Queries.SearchPages;

public sealed record SearchPagesQuery(
    Guid WorkspaceId,
    string Keyword,
    int PageNumber = 1,
    int PageSize = 20);