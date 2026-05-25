using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.ListRecentPages;

public sealed class ListRecentPagesHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageUserStateRepository _pageUserStateRepository;

    public ListRecentPagesHandler(ICurrentUser currentUser, IPageUserStateRepository pageUserStateRepository)
    {
        _currentUser = currentUser;
        _pageUserStateRepository = pageUserStateRepository;
    }

    public async Task<Result<PageQuickAccessPagedResultDto>> HandleAsync(ListRecentPagesQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageQuickAccessPagedResultDto>(PageErrors.MissingUserContext);

        var page = NormalizePage(request.PageNumber);
        var size = NormalizeSize(request.PageSize);
        var items = await _pageUserStateRepository.ListRecentAsync(currentUserId, page, size, cancellationToken);
        var total = await _pageUserStateRepository.CountRecentAsync(currentUserId, cancellationToken);

        return Result.Success(new PageQuickAccessPagedResultDto(items, page, size, total, CalculateTotalPages(total, size)));
    }

    private static int NormalizePage(int pageNumber) => pageNumber <= 0 ? 1 : pageNumber;
    private static int NormalizeSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
    private static int CalculateTotalPages(int total, int size) => total <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}
