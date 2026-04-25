using Pkm.Application.Abstractions.Caching;

namespace Pkm.Application.Features.Recommendations;

public static class RecommendationCacheKeys
{
    public static string UserThrottle(
        IRedisKeyFactory keyFactory,
        Guid workspaceId,
        Guid userId)
        => keyFactory.Build(
            "recommendations",
            "throttle",
            workspaceId.ToString("D"),
            userId.ToString("D"));

    public static string UserPendingVersion(
        IRedisKeyFactory keyFactory,
        Guid userId)
        => keyFactory.Build(
            "recommendations",
            "pending-version",
            userId.ToString("D"));

    public static string PendingList(
        IRedisKeyFactory keyFactory,
        Guid userId,
        Guid? workspaceId,
        string status,
        int pageNumber,
        int pageSize,
        string version)
        => keyFactory.Build(
            "recommendations",
            "pending-list",
            userId.ToString("D"),
            workspaceId?.ToString("D") ?? "all",
            status,
            pageNumber.ToString(),
            pageSize.ToString(),
            version);

    public static string Preference(
        IRedisKeyFactory keyFactory,
        Guid workspaceId,
        Guid userId)
        => keyFactory.Build(
            "recommendations",
            "preference",
            workspaceId.ToString("D"),
            userId.ToString("D"));

    public static string HistoryStats(
        IRedisKeyFactory keyFactory,
        Guid workspaceId,
        Guid userId)
        => keyFactory.Build(
            "recommendations",
            "history-stats",
            workspaceId.ToString("D"),
            userId.ToString("D"));
}