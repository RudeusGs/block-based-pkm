using Pkm.Application.Features.Notifications.Models;
using Pkm.Domain.Notifications;

namespace Pkm.Application.Features.Notifications.Services;

public interface INotificationService
{
    Task<NotificationDto?> NotifyAsync(
        Guid recipientUserId,
        NotificationDispatchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> NotifyManyAsync(
        IEnumerable<Guid> recipientUserIds,
        NotificationDispatchRequest request,
        IEnumerable<Guid>? excludeUserIds = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> NotifyWorkspaceAsync(
        Guid workspaceId,
        NotificationDispatchRequest request,
        IEnumerable<Guid>? excludeUserIds = null,
        CancellationToken cancellationToken = default);

    Task InvalidateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task PublishUnreadCountChangedAsync(
        Guid userId,
        Guid? workspaceId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationDispatchRequest(
    NotificationType Type,
    string Title,
    string Message,
    Guid? ActorUserId = null,
    Guid? WorkspaceId = null,
    Guid? ReferenceId = null,
    string? ReferenceType = null);