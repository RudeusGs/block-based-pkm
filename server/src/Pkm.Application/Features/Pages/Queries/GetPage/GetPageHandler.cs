using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Pages.Queries.GetPage;

public sealed class GetPageHandler : IQueryHandler<GetPageQuery, PageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageReadRepository _pageRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageUserStateRepository _pageUserStateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GetPageHandler(
        ICurrentUser currentUser,
        IPageReadRepository pageRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageUserStateRepository pageUserStateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageUserStateRepository = pageUserStateRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
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

        await RecordRecentPageAsync(currentUserId, page.Id, cancellationToken);

        return Result.Success(page.ToDto());
    }

    private async Task RecordRecentPageAsync(Guid userId, Guid pageId, CancellationToken cancellationToken)
    {
        var recent = await _pageUserStateRepository.GetRecentAsync(userId, pageId, cancellationToken);

        if (recent is null)
        {
            _pageUserStateRepository.AddRecent(new PageRecent(Guid.NewGuid(), userId, pageId, _clock.UtcNow));
        }
        else
        {
            recent.MarkVisited(_clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
