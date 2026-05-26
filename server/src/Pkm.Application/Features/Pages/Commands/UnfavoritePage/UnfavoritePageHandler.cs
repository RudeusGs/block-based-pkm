using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Pages.Commands.UnfavoritePage;

public sealed class UnfavoritePageHandler : ICommandHandler<UnfavoritePageCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageUserStateRepository _pageUserStateRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UnfavoritePageHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageUserStateRepository pageUserStateRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageUserStateRepository = pageUserStateRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UnfavoritePageCommand request, CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(request.PageId, currentUserId, cancellationToken);
        if (!access.Exists)
            return Result.Failure(PageErrors.PageNotFound);

        if (!access.CanRead)
            return Result.Failure(PageErrors.PageForbidden);

        var favorite = await _pageUserStateRepository.GetFavoriteAsync(currentUserId, request.PageId, cancellationToken);
        if (favorite is not null)
        {
            _pageUserStateRepository.RemoveFavorite(favorite);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
