using Pkm.Application.Common.Abstractions.Caching;

namespace Pkm.Application.Features.Workspaces;

public static class WorkspaceCacheKeys
{
    public static string Detail(ICacheKeyFactory keyFactory, Guid workspaceId)
        => keyFactory.Build("workspace", "detail", workspaceId.ToString());

    public static string Members(ICacheKeyFactory keyFactory, Guid workspaceId)
        => keyFactory.Build("workspace", "members", workspaceId.ToString());

    public static string Access(ICacheKeyFactory keyFactory, Guid workspaceId, Guid userId)
        => keyFactory.Build("workspace", workspaceId.ToString(), "access", userId.ToString());

    public static string List(ICacheKeyFactory keyFactory, Guid userId, int page, int size, string version)
        => keyFactory.Build("user", userId.ToString(), "workspaces", $"v{version}", $"p{page}", $"s{size}");

    public static string UserListVersion(ICacheKeyFactory keyFactory, Guid userId)
        => keyFactory.Build("user", userId.ToString(), "workspaces", "version");
}
