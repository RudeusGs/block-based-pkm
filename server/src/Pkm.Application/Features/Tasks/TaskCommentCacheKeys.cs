using System.Globalization;
using Pkm.Application.Abstractions.Caching;

namespace Pkm.Application.Features.Tasks;

public static class TaskCommentCacheKeys
{
    public static string ListVersion(IRedisKeyFactory keyFactory, Guid taskId)
        => keyFactory.Build(
            "tasks",
            taskId.ToString("D"),
            "comments",
            "version");

    public static string List(
        IRedisKeyFactory keyFactory,
        Guid taskId,
        int pageNumber,
        int pageSize,
        bool includeDeleted,
        string version)
        => keyFactory.Build(
            "tasks",
            taskId.ToString("D"),
            "comments",
            "list",
            includeDeleted ? "with-deleted" : "active",
            pageNumber.ToString(CultureInfo.InvariantCulture),
            pageSize.ToString(CultureInfo.InvariantCulture),
            version);
}