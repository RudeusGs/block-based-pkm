using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListRecentPages;

public sealed record ListRecentPagesQuery(
    int PageNumber,
    int PageSize) : IQuery<PageQuickAccessPagedResultDto>;
