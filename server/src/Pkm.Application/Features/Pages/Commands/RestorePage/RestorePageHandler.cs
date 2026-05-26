using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Pages.Realtime;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Pages.Commands.RestorePage;

public sealed class RestorePageHandler : ICommandHandler<RestorePageCommand, PageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IActivityLogService _activityLogService;
    private readonly IPageRealtimePublisher _pageRealtimePublisher;

    public RestorePageHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageWriteRepository pageWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IActivityLogService activityLogService,
        IPageRealtimePublisher pageRealtimePublisher)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageWriteRepository = pageWriteRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _activityLogService = activityLogService;
        _pageRealtimePublisher = pageRealtimePublisher;
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

        var page = await _pageWriteRepository.GetByIdIncludingArchivedForUpdateAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        try
        {
            var now = _clock.UtcNow;

            page.RestoreFromArchive(currentUserId, now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = page.ToDto();

            await _pageRealtimePublisher.PublishWorkspacePageChangedAsync(
                PageRealtimeEventNames.Restored,
                dto,
                currentUserId,
                now,
                cancellationToken);
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



