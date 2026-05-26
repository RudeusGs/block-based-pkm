using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Pages;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Pages.Realtime;
using Pkm.Domain.Audit;
namespace Pkm.Application.Features.Pages.Commands.CreatePage;

public sealed class CreatePageHandler : ICommandHandler<CreatePageCommand, PageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageReadRepository _pageReadRepository;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IPageRealtimePublisher _pageRealtimePublisher;
    private readonly IActivityLogService _activityLogService;
    public CreatePageHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageReadRepository pageReadRepository,
        IPageWriteRepository pageWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IPageRealtimePublisher pageRealtimePublisher,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageReadRepository = pageReadRepository;
        _pageWriteRepository = pageWriteRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _pageRealtimePublisher = pageRealtimePublisher;
        _activityLogService = activityLogService;
    }

    public async Task<Result<PageDto>> HandleAsync(
        CreatePageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
            return Result.Failure<PageDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDto>(PageErrors.MissingUserContext);

        var workspaceAccess = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!workspaceAccess.Exists)
            return Result.Failure<PageDto>(WorkspaceErrors.WorkspaceNotFound);

        if (request.ParentPageId.HasValue)
        {
            var parentPage = await _pageReadRepository.GetByIdAsync(
                request.ParentPageId.Value,
                cancellationToken);

            if (parentPage is null)
                return Result.Failure<PageDto>(PageErrors.ParentPageNotFound);

            if (parentPage.WorkspaceId != request.WorkspaceId)
                return Result.Failure<PageDto>(PageErrors.ParentPageDifferentWorkspace);

            var parentAccess = await _pageAccessEvaluator.EvaluateAsync(
                request.ParentPageId.Value,
                currentUserId,
                cancellationToken);

            if (!parentAccess.Exists)
                return Result.Failure<PageDto>(PageErrors.ParentPageNotFound);

            if (!parentAccess.CanCreateSubPage)
                return Result.Failure<PageDto>(PageErrors.PageForbidden);
        }
        else
        {
            if (!workspaceAccess.CanCreatePages)
                return Result.Failure<PageDto>(WorkspaceErrors.WorkspaceForbidden);
        }

        try
        {
            var now = _clock.UtcNow;

            var page = new Page(
                Guid.NewGuid(),
                request.WorkspaceId,
                request.Title,
                currentUserId,
                now,
                request.ParentPageId,
                request.Icon,
                request.CoverImage);

            _pageWriteRepository.Add(page);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    page.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.Page,
                    page.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã tạo page \"{page.Title}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        title = page.Title,
                        parentPageId = page.ParentPageId,
                        icon = page.Icon
                    })),
                cancellationToken);

            var dto = page.ToDto();

            await _pageRealtimePublisher.PublishWorkspacePageChangedAsync(
                PageRealtimeEventNames.Created,
                dto,
                currentUserId,
                now,
                cancellationToken);

            await _notificationService.NotifyWorkspaceAsync(
                page.WorkspaceId,
                NotificationTemplates.PageCreated(
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
                "Page.CreateFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}



