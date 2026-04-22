using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Pages.Queries.GetPage;

public sealed class GetPageHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;

    public GetPageHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IPageAccessEvaluator pageAccessEvaluator)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
    }

    public async Task<Result<PageDto>> HandleAsync(
        GetPageQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PageDto>(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDto>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        if (!access.CanRead)
            return Result.Failure<PageDto>(PageErrors.PageForbidden);

        var page = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        return Result.Success(page.ToDto());
    }
}