using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Services;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Pages;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Files.Commands.UploadPageCoverImage;

public sealed class UploadPageCoverImageHandler : ICommandHandler<UploadPageCoverImageCommand, PageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IDocumentRealtimePublisher _realtimePublisher;

    public UploadPageCoverImageHandler(
        ICurrentUser currentUser,
        IPageWriteRepository pageWriteRepository,
        IPageRevisionRepository pageRevisionRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IFileUploadApplicationService fileUploadApplicationService,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IDocumentRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _pageWriteRepository = pageWriteRepository;
        _pageRevisionRepository = pageRevisionRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _fileUploadApplicationService = fileUploadApplicationService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result<PageDto>> HandleAsync(
        UploadPageCoverImageCommand request,
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

        if (request.ExpectedRevision.HasValue)
        {
            var revisionError = DocumentRevisionGuard.ValidateExpectedRevision(
                page,
                request.ExpectedRevision.Value);

            if (revisionError is not null)
                return Result.Failure<PageDto>(revisionError);
        }

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(
                currentUserId,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.Content,
                "page-cover"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<PageDto>(uploadResult.Error);

        try
        {
            var now = _clock.UtcNow;
            var oldCoverImage = page.CoverImage;

            page.UpdateAppearance(page.Icon, uploadResult.Value.PublicUrl, currentUserId, now);

            if (oldCoverImage == page.CoverImage)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Success(page.ToDto());
            }

            var appliedRevision = page.IncreaseRevision(currentUserId, now);

            _pageRevisionRepository.Add(PageRevision.Create(
                page.Id,
                appliedRevision,
                currentUserId,
                now,
                "Page cover image uploaded"));

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
                "File.UpdatePageCoverFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}
