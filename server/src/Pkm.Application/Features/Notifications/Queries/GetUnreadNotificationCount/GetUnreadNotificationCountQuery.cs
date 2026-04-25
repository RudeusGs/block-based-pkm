namespace Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed record GetUnreadNotificationCountQuery(
    Guid? WorkspaceId = null);