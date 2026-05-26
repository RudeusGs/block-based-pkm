using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Pages.Queries.ListSubPages;

public sealed class ListSubPagesHandler : IQueryHandler<ListSubPagesQuery, PagePagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageReadRepository _pageRepository;

    public ListSubPagesHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageReadRepository pageRepository)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageRepository = pageRepository;
    }

    public async Task<Result<PagePagedResultDto>> HandleAsync(
        ListSubPagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ParentPageId == Guid.Empty)
            return Result.Failure<PagePagedResultDto>(PageErrors.InvalidPageId(request.ParentPageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PagePagedResultDto>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.ParentPageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PagePagedResultDto>(PageErrors.PageNotFound);

        if (!access.CanRead)
            return Result.Failure<PagePagedResultDto>(PageErrors.PageForbidden);

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize);

        var items = await _pageRepository.ListSubPagesAsync(
            request.ParentPageId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _pageRepository.CountSubPagesAsync(
            request.ParentPageId,
            cancellationToken);

        return Result.Success(new PagePagedResultDto(
            items.Select(x => x.ToDto()).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize)));
    }
}
