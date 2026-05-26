using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListFavoritePages;

public sealed record ListFavoritePagesQuery(
    int PageNumber,
    int PageSize) : IQuery<PageQuickAccessPagedResultDto>;
