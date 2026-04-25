namespace Pkm.Application.Abstractions.Realtime;

public interface INotificationRealtimePublisher
{
    Task PublishToUserAsync(
        NotificationRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationRealtimeEnvelope(
    string EventName,
    Guid UserId,
    Guid? WorkspaceId,
    Guid? ActorId,
    DateTimeOffset OccurredAtUtc,
    object? Payload);