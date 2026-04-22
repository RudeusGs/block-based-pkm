using Pkm.Application.Abstractions.Caching;

namespace Pkm.Application.Features.Workspaces;

public static class WorkspaceCacheKeys
{
    public static string Detail(IRedisKeyFactory keyFactory, Guid workspaceId)
        => keyFactory.Build("workspace", "detail", workspaceId.ToString());

    public static string Members(IRedisKeyFactory keyFactory, Guid workspaceId)
        => keyFactory.Build("workspace", "members", workspaceId.ToString());

    public static string Access(IRedisKeyFactory keyFactory, Guid workspaceId, Guid userId)
        => keyFactory.Build("workspace", workspaceId.ToString(), "access", userId.ToString());

    public static string List(IRedisKeyFactory keyFactory, Guid userId, int page, int size, string version)
        => keyFactory.Build("user", userId.ToString(), "workspaces", $"v{version}", $"p{page}", $"s{size}");

    public static string UserListVersion(IRedisKeyFactory keyFactory, Guid userId)
        => keyFactory.Build("user", userId.ToString(), "workspaces", "version");
}