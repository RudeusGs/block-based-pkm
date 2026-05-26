using Pkm.Application.Common.Abstractions.Caching;

namespace Pkm.Application.Features.Messaging;

public static class MessagingCacheKeys
{
    public static string ConversationListVersion(ICacheKeyFactory keyFactory, Guid userId)
        => keyFactory.Build("user", userId.ToString(), "conversations", "version");

    public static string ConversationList(ICacheKeyFactory keyFactory, Guid userId, int page, int size, string version)
        => keyFactory.Build("user", userId.ToString(), "conversations", $"v{version}", $"p{page}", $"s{size}");
}
