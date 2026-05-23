using Pkm.Application.Abstractions.Caching;

namespace Pkm.Application.Features.Messaging;

public static class MessagingCacheKeys
{
    public static string ConversationListVersion(IRedisKeyFactory keyFactory, Guid userId)
        => keyFactory.Build("user", userId.ToString(), "conversations", "version");

    public static string ConversationList(IRedisKeyFactory keyFactory, Guid userId, int page, int size, string version)
        => keyFactory.Build("user", userId.ToString(), "conversations", $"v{version}", $"p{page}", $"s{size}");
}
