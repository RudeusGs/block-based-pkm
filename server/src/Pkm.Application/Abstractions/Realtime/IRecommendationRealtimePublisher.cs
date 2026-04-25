namespace Pkm.Application.Abstractions.Realtime;

public interface IRecommendationRealtimePublisher
{
    Task PublishToUserAsync(
        RecommendationRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task PublishToWorkspaceAsync(
        RecommendationRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public sealed record RecommendationRealtimeEnvelope(
    string EventName,
    Guid UserId,
    Guid WorkspaceId,
    Guid? PageId,
    Guid? ActorId,
    DateTimeOffset OccurredAtUtc,
    object? Payload);