using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Pages.Realtime;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Audit;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;

public sealed class UpdatePageMetadataHandler : ICommandHandler<UpdatePageMetadataCommand, PageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IPageRealtimePublisher _pageRealtimePublisher;
    private readonly IActivityLogService _activityLogService;

    public UpdatePageMetadataHandler(
        ICurrentUser currentUser,
        IPageWriteRepository pageWriteRepository,
        IPageRevisionRepository pageRevisionRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IPageRealtimePublisher pageRealtimePublisher,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _pageWriteRepository = pageWriteRepository;
        _pageRevisionRepository = pageRevisionRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _pageRealtimePublisher = pageRealtimePublisher;
        _activityLogService = activityLogService;
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

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(
            request.PageId,
            cancellationToken);

        if (page is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(
            page,
            request.ExpectedRevision);

        if (revisionError is not null)
            return Result.Failure<PageDto>(revisionError);

        var oldTitle = page.Title;
        var oldIcon = page.Icon;
        var oldCoverImage = page.CoverImage;

        try
        {
            var now = _clock.UtcNow;

            page.Rename(request.Title, currentUserId, now);
            page.UpdateAppearance(request.Icon, request.CoverImage, currentUserId, now);

            var hasChanged =
                oldTitle != page.Title ||
                oldIcon != page.Icon ||
                oldCoverImage != page.CoverImage;

            if (!hasChanged)
                return Result.Success(page.ToDto());

            var appliedRevision = page.IncreaseRevision(currentUserId, now);

            _pageRevisionRepository.Add(PageRevision.Create(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Page metadata updated"));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var dto = page.ToDto();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    page.WorkspaceId,
                    currentUserId,
                    ActivityAction.Update,
                    ActivityEntityType.Page,
                    page.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã cập nhật page \"{page.Title}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        oldTitle,
                        newTitle = page.Title,
                        oldIcon,
                        newIcon = page.Icon,
                        oldCoverImage,
                        newCoverImage = page.CoverImage,
                        revision = appliedRevision
                    })),
                cancellationToken);

            await _pageRealtimePublisher.PublishWorkspacePageChangedAsync(
                PageRealtimeEventNames.MetadataUpdated,
                dto,
                currentUserId,
                now,
                cancellationToken);

            await _notificationService.NotifyWorkspaceAsync(
                page.WorkspaceId,
                NotificationTemplates.PageUpdated(
                    currentUserId,
                    _currentUser.UserName ?? "Có người",
                    page.WorkspaceId,
                    page.Id,
                    page.Title),
                excludeUserIds: new[] { currentUserId },
                cancellationToken);

            return Result.Success(dto);
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



