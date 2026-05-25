using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand(Guid? WorkspaceId = null) : ICommand<NotificationUnreadCountDto>;
