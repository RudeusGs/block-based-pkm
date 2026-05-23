namespace Pkm.Application.Abstractions.Realtime;

public interface ISocialRealtimePublisher
{
    Task PublishToUserAsync(
        SocialRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task PublishToConversationAsync(
        MessagingRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);
}
