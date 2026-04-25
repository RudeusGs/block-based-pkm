namespace Pkm.Application.Features.Notifications.Queries.ListNotifications;

public sealed record ListNotificationsQuery(
    Guid? WorkspaceId = null,
    bool UnreadOnly = false,
    int PageNumber = 1,
    int PageSize = 20);