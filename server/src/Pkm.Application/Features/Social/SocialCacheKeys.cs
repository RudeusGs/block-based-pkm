using Pkm.Application.Common.Abstractions.Caching;

namespace Pkm.Application.Features.Social;

public static class SocialCacheKeys
{
    public static string FriendListVersion(IRedisKeyFactory keyFactory, Guid userId)
        => keyFactory.Build("user", userId.ToString(), "friends", "version");

    public static string FriendList(IRedisKeyFactory keyFactory, Guid userId, int page, int size, string version)
        => keyFactory.Build("user", userId.ToString(), "friends", $"v{version}", $"p{page}", $"s{size}");

    public static string UserSearch(IRedisKeyFactory keyFactory, Guid viewerId, string keyword, int page, int size)
        => keyFactory.Build("social", "search", viewerId.ToString(), keyword.ToUpperInvariant(), $"p{page}", $"s{size}");
}
