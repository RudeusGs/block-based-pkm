using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListFavoritePages;

public sealed class ListFavoritePagesHandler : IQueryHandler<ListFavoritePagesQuery, PageQuickAccessPagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageUserStateRepository _pageUserStateRepository;

    public ListFavoritePagesHandler(ICurrentUser currentUser, IPageUserStateRepository pageUserStateRepository)
    {
        _currentUser = currentUser;
        _pageUserStateRepository = pageUserStateRepository;
    }

    public async Task<Result<PageQuickAccessPagedResultDto>> HandleAsync(ListFavoritePagesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageQuickAccessPagedResultDto>(PageErrors.MissingUserContext);

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize);
        var items = await _pageUserStateRepository.ListFavoritesAsync(currentUserId, page.PageNumber, page.PageSize, cancellationToken);
        var total = await _pageUserStateRepository.CountFavoritesAsync(currentUserId, cancellationToken);

        return Result.Success(new PageQuickAccessPagedResultDto(
            items,
            page.PageNumber,
            page.PageSize,
            total,
            PageRequest.CalculateTotalPages(total, page.PageSize)));
    }
}
