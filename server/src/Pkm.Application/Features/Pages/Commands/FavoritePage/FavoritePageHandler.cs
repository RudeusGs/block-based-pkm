using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Pages.Commands.FavoritePage;

public sealed class FavoritePageHandler : ICommandHandler<FavoritePageCommand, PageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageReadRepository _pageReadRepository;
    private readonly IPageUserStateRepository _pageUserStateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public FavoritePageHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageReadRepository pageReadRepository,
        IPageUserStateRepository pageUserStateRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageReadRepository = pageReadRepository;
        _pageUserStateRepository = pageUserStateRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PageDto>> HandleAsync(FavoritePageCommand request, CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PageDto>(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDto>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(request.PageId, currentUserId, cancellationToken);
        if (!access.Exists)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        if (!access.CanRead)
            return Result.Failure<PageDto>(PageErrors.PageForbidden);

        var page = await _pageReadRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        var existing = await _pageUserStateRepository.GetFavoriteAsync(currentUserId, request.PageId, cancellationToken);
        if (existing is null)
        {
            _pageUserStateRepository.AddFavorite(new PageFavorite(Guid.NewGuid(), currentUserId, request.PageId, _clock.UtcNow));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(page.ToDto());
    }
}
