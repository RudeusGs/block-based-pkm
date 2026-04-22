using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Pages.Commands.DeletePage;

public sealed class DeletePageHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeletePageHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(
        DeletePageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure(PageErrors.PageNotFound);

        if (!access.CanArchivePage)
            return Result.Failure(PageErrors.PageForbidden);

        var page = await _pageRepository.GetByIdForUpdateAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure(PageErrors.PageNotFound);

        page.SoftDelete(_clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}