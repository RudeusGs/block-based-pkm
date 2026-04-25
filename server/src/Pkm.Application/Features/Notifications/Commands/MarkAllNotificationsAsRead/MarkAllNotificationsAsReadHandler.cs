using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications.Models;
using Pkm.Application.Features.Notifications.Services;

namespace Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed class MarkAllNotificationsAsReadHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;

    public MarkAllNotificationsAsReadHandler(
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
    }

    public async Task<Result<NotificationUnreadCountDto>> HandleAsync(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<NotificationUnreadCountDto>(
                NotificationErrors.InvalidReadRequest(new[] { "WorkspaceId không hợp lệ." }));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<NotificationUnreadCountDto>(
                NotificationErrors.MissingUserContext);
        }

        var unreadNotifications = await _notificationRepository.ListUnreadByUserAsync(
            currentUserId,
            request.WorkspaceId,
            cancellationToken);

        var now = _clock.UtcNow;

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead(now);
            _notificationRepository.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.InvalidateUserAsync(
            currentUserId,
            cancellationToken);

        await _notificationService.PublishUnreadCountChangedAsync(
            currentUserId,
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        return Result.Success(new NotificationUnreadCountDto(
            currentUserId,
            request.WorkspaceId,
            0));
    }
}