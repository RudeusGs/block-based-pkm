using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Pages.Commands.RestorePage;

public sealed class RestorePageHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IActivityLogService _activityLogService;

    public RestorePageHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageRepository pageRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _activityLogService = activityLogService;
    }

    public async Task<Result<PageDto>> HandleAsync(RestorePageCommand request, CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PageDto>(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDto>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(request.PageId, currentUserId, cancellationToken);
        if (!access.Exists)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        if (!access.CanManagePage)
            return Result.Failure<PageDto>(PageErrors.PageForbidden);

        var page = await _pageRepository.GetByIdIncludingArchivedForUpdateAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        try
        {
            page.RestoreFromArchive(currentUserId, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<PageDto>(new Error("Page.RestoreFailed", ex.Message, ResultStatus.Unprocessable));
        }

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                page.WorkspaceId,
                currentUserId,
                ActivityAction.Restore,
                ActivityEntityType.Page,
                page.Id,
                $"{_currentUser.UserName ?? "Có người"} đã restore page \"{page.Title}\".",
                ActivityLogMetadata.Serialize(new { pageId = page.Id, title = page.Title })),
            cancellationToken);

        return Result.Success(page.ToDto());
    }
}
