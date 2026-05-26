using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Pages.Queries.ListSubPages;

public sealed class ListSubPagesHandler : IQueryHandler<ListSubPagesQuery, IReadOnlyList<PageDto>>
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

    public async Task<Result<IReadOnlyList<PageDto>>> HandleAsync(
        ListSubPagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.ParentPageId == Guid.Empty)
            return Result.Failure<IReadOnlyList<PageDto>>(PageErrors.InvalidPageId(request.ParentPageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<IReadOnlyList<PageDto>>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.ParentPageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<IReadOnlyList<PageDto>>(PageErrors.PageNotFound);

        if (!access.CanRead)
            return Result.Failure<IReadOnlyList<PageDto>>(PageErrors.PageForbidden);

        var items = await _pageRepository.ListSubPagesAsync(request.ParentPageId, cancellationToken);

        return Result.Success<IReadOnlyList<PageDto>>(items.Select(x => x.ToDto()).ToArray());
    }
}
