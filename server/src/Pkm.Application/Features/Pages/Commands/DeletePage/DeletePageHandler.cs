using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Pages.Commands.DeletePage;

public sealed class DeletePageHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLogService;
    public DeletePageHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _clock = clock;
        _activityLogService = activityLogService;
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

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                page.WorkspaceId,
                currentUserId,
                ActivityAction.Delete,
                ActivityEntityType.Page,
                page.Id,
                $"{_currentUser.UserName ?? "Có người"} đã xóa page \"{page.Title}\".",
                ActivityLogMetadata.Serialize(new
                {
                    pageId = page.Id,
                    title = page.Title,
                    parentPageId = page.ParentPageId
                })),
            cancellationToken);

        await _notificationService.NotifyWorkspaceAsync(
            page.WorkspaceId,
            NotificationTemplates.PageDeleted(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                page.WorkspaceId,
                page.Id,
                page.Title),
            excludeUserIds: new[] { currentUserId },
            cancellationToken);
        return Result.Success();
    }
}
