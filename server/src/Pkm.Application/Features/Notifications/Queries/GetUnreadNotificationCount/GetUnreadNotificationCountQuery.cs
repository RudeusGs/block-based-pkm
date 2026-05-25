using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery(Guid? WorkspaceId = null) : IQuery<NotificationUnreadCountDto>;
