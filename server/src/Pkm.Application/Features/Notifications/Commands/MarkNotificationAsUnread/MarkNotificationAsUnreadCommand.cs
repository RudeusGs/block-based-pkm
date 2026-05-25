using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Application.Features.Notifications.Commands.MarkNotificationAsUnread;

public sealed record MarkNotificationAsUnreadCommand(Guid NotificationId) : ICommand<NotificationDto>;
