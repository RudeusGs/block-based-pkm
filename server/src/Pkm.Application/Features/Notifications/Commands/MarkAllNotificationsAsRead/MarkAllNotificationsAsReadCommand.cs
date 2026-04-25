namespace Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand(Guid? WorkspaceId = null);