namespace Pkm.Application.Features.Notifications.Models;

public sealed record NotificationUnreadCountDto(
    Guid UserId,
    Guid? WorkspaceId,
    int UnreadCount);