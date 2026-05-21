using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;

public sealed class UpdatePageMetadataHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IDocumentRealtimePublisher _realtimePublisher;

    public UpdatePageMetadataHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IPageRevisionRepository pageRevisionRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IDocumentRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _pageRevisionRepository = pageRevisionRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _realtimePublisher = realtimePublisher;
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

        var page = await _pageRepository.GetByIdForUpdateAsync(
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

            await _realtimePublisher.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "PageMetadataUpdated",
                    WorkspaceId: page.WorkspaceId,
                    PageId: page.Id,
                    BlockId: null,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Revision: appliedRevision,
                    Payload: dto),
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