using System.Globalization;
using Pkm.Application.Abstractions.Caching;

namespace Pkm.Application.Features.Notifications;

public static class NotificationCacheKeys
{
    public static string ListVersion(
        IRedisKeyFactory keyFactory,
        Guid userId)
        => keyFactory.Build(
            "notifications",
            "users",
            userId.ToString("D"),
            "list",
            "version");

    public static string List(
        IRedisKeyFactory keyFactory,
        Guid userId,
        Guid? workspaceId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        string version)
        => keyFactory.Build(
            "notifications",
            "users",
            userId.ToString("D"),
            "list",
            WorkspaceSegment(workspaceId),
            unreadOnly ? "unread" : "all",
            pageNumber.ToString(CultureInfo.InvariantCulture),
            pageSize.ToString(CultureInfo.InvariantCulture),
            version);

    public static string UnreadCountVersion(
        IRedisKeyFactory keyFactory,
        Guid userId)
        => keyFactory.Build(
            "notifications",
            "users",
            userId.ToString("D"),
            "unread-count",
            "version");

    public static string UnreadCount(
        IRedisKeyFactory keyFactory,
        Guid userId,
        Guid? workspaceId,
        string version)
        => keyFactory.Build(
            "notifications",
            "users",
            userId.ToString("D"),
            "unread-count",
            WorkspaceSegment(workspaceId),
            version);

    private static string WorkspaceSegment(Guid? workspaceId)
        => workspaceId.HasValue && workspaceId.Value != Guid.Empty
            ? workspaceId.Value.ToString("D")
            : "all";
}