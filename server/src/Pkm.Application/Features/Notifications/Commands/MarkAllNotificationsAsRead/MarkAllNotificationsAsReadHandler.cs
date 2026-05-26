using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications.Models;
using Pkm.Application.Features.Notifications.Services;

namespace Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed class MarkAllNotificationsAsReadHandler : ICommandHandler<MarkAllNotificationsAsReadCommand, NotificationUnreadCountDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly INotificationRepository _notificationRepository;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;

    public MarkAllNotificationsAsReadHandler(
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IClock clock,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
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

        var updatedCount = await _notificationRepository.MarkUnreadAsReadAsync(
            currentUserId,
            request.WorkspaceId,
            _clock.UtcNow,
            cancellationToken);

        if (updatedCount > 0)
        {
            await _notificationService.InvalidateUserAsync(
                currentUserId,
                cancellationToken);
        }

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
