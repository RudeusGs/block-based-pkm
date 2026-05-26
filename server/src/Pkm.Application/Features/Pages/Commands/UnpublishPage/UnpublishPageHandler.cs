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

namespace Pkm.Application.Features.Pages.Commands.UnpublishPage;

public sealed class UnpublishPageHandler : ICommandHandler<UnpublishPageCommand, PagePublishDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageRealtimePublisher _pageRealtimePublisher;
    private readonly IActivityLogService _activityLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnpublishPageHandler(
        ICurrentUser currentUser,
        IPageWriteRepository pageWriteRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageRealtimePublisher pageRealtimePublisher,
        IActivityLogService activityLogService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _pageWriteRepository = pageWriteRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageRealtimePublisher = pageRealtimePublisher;
        _activityLogService = activityLogService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PagePublishDto>> HandleAsync(
        UnpublishPageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PagePublishDto>(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PagePublishDto>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PagePublishDto>(PageErrors.PageNotFound);

        if (!access.CanManagePage)
            return Result.Failure<PagePublishDto>(PageErrors.PageForbidden);

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(
            request.PageId,
            cancellationToken);

        if (page is null)
            return Result.Failure<PagePublishDto>(PageErrors.PageNotFound);

        if (!page.IsPublished && string.IsNullOrWhiteSpace(page.PublicToken))
            return Result.Success(ToPublishDto(page));

        try
        {
            var now = _clock.UtcNow;
            page.Unpublish(currentUserId, now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var pageDto = page.ToDto();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    page.WorkspaceId,
                    currentUserId,
                    ActivityAction.Update,
                    ActivityEntityType.Page,
                    page.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã tắt public link của page \"{page.Title}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        isPublished = false
                    })),
                cancellationToken);

            await _pageRealtimePublisher.PublishWorkspacePageChangedAsync(
                PageRealtimeEventNames.MetadataUpdated,
                pageDto,
                currentUserId,
                now,
                cancellationToken);

            return Result.Success(ToPublishDto(page));
        }
        catch (DomainException ex)
        {
            return Result.Failure<PagePublishDto>(new Error(
                "Page.UnpublishFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private static PagePublishDto ToPublishDto(Pkm.Domain.Pages.Page page)
        => new(
            page.Id,
            page.IsPublished,
            page.PublicToken,
            page.PublishedAt,
            page.PublishedBy);
}
