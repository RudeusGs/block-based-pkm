using Pkm.Domain.Notifications;

namespace Pkm.Application.Features.Notifications.Models;

public static class NotificationMappings
{
    public static NotificationDto ToDto(this Notification notification)
        => new(
            notification.Id,
            notification.UserId,
            notification.WorkspaceId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.ReferenceId,
            notification.ReferenceType,
            notification.IsRead,
            notification.ReadAtUtc,
            notification.CreatedDate,
            notification.UpdatedDate);
}