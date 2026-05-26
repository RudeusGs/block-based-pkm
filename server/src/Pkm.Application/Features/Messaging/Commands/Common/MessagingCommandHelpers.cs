using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

internal static class MessagingCommandHelpers
{
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    public static async Task InvalidateConversationListsAsync(
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken)
    {
        foreach (var userId in new[] { userAId, userBId }.Where(x => x != Guid.Empty).Distinct())
        {
            await cache.SetAsync(
                MessagingCacheKeys.ConversationListVersion(cacheKeyFactory, userId),
                Guid.NewGuid().ToString("N"),
                VersionTtl,
                cancellationToken);
        }
    }

    public static async Task PublishConversationEventAsync(
        ISocialRealtimePublisher realtimePublisher,
        IClock clock,
        Guid conversationId,
        Guid senderId,
        Guid recipientId,
        string eventName,
        object payload,
        CancellationToken cancellationToken)
    {
        await realtimePublisher.PublishToConversationAsync(
            new MessagingRealtimeEnvelope(eventName, conversationId, senderId, recipientId, clock.UtcNow, payload),
            cancellationToken);
    }
}
