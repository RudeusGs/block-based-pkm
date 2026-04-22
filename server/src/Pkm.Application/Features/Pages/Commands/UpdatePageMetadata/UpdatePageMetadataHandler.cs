using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Common;

namespace Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;

public sealed class UpdatePageMetadataHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePageMetadataHandler(
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

    public async Task<Result<PageDto>> HandleAsync(
        UpdatePageMetadataCommand request,
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

        if (!access.CanEditPageMetadata)
            return Result.Failure<PageDto>(PageErrors.PageForbidden);

        var page = await _pageRepository.GetByIdForUpdateAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        try
        {
            var now = _clock.UtcNow;

            page.Rename(request.Title, currentUserId, now);
            page.UpdateAppearance(request.Icon, request.CoverImage, currentUserId, now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(page.ToDto());
        }
        catch (DomainException ex)
        {
            return Result.Failure<PageDto>(new Error(
                "Page.UpdateFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}