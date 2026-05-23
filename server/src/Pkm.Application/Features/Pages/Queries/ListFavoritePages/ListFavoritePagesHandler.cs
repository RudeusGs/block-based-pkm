using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListFavoritePages;

public sealed class ListFavoritePagesHandler
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

        var page = NormalizePage(request.PageNumber);
        var size = NormalizeSize(request.PageSize);
        var items = await _pageUserStateRepository.ListFavoritesAsync(currentUserId, page, size, cancellationToken);
        var total = await _pageUserStateRepository.CountFavoritesAsync(currentUserId, cancellationToken);

        return Result.Success(new PageQuickAccessPagedResultDto(items, page, size, total, CalculateTotalPages(total, size)));
    }

    private static int NormalizePage(int pageNumber) => pageNumber <= 0 ? 1 : pageNumber;
    private static int NormalizeSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
    private static int CalculateTotalPages(int total, int size) => total <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}
