using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Notifications.Commands.DeleteNotification;

public sealed record DeleteNotificationCommand(Guid NotificationId) : ICommand;
