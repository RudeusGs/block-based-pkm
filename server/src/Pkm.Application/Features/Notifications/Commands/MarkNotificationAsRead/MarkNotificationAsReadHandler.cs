using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications.Models;
using Pkm.Application.Features.Notifications.Services;

namespace Pkm.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly INotificationRealtimePublisher _notificationRealtimePublisher;

    public MarkNotificationAsReadHandler(
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        INotificationRealtimePublisher notificationRealtimePublisher)
    {
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _notificationRealtimePublisher = notificationRealtimePublisher;
    }

    public async Task<Result<NotificationDto>> HandleAsync(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        if (request.NotificationId == Guid.Empty)
        {
            return Result.Failure<NotificationDto>(
                NotificationErrors.InvalidReadRequest(new[] { "NotificationId không hợp lệ." }));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<NotificationDto>(
                NotificationErrors.MissingUserContext);
        }

        var notification = await _notificationRepository.GetByIdForUserAsync(
            request.NotificationId,
            currentUserId,
            cancellationToken);

        if (notification is null)
            return Result.Failure<NotificationDto>(NotificationErrors.NotificationNotFound);

        var now = _clock.UtcNow;

        notification.MarkAsRead(now);
        _notificationRepository.Update(notification);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.InvalidateUserAsync(
            currentUserId,
            cancellationToken);

        var dto = notification.ToDto();

        await _notificationRealtimePublisher.PublishToUserAsync(
            new NotificationRealtimeEnvelope(
                EventName: "NotificationReadChanged",
                UserId: currentUserId,
                WorkspaceId: notification.WorkspaceId,
                ActorId: currentUserId,
                OccurredAtUtc: now,
                Payload: dto),
            cancellationToken);

        await _notificationService.PublishUnreadCountChangedAsync(
            currentUserId,
            notification.WorkspaceId,
            currentUserId,
            cancellationToken);

        return Result.Success(dto);
    }
}