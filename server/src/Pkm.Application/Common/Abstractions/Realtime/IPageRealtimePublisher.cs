namespace Pkm.Application.Common.Abstractions.Realtime;

public interface IPageRealtimePublisher
{
    Task PublishToWorkspaceAsync(
        PageRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task PublishToPageAsync(
        PageRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task PublishToUserAsync(
        PageUserRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public sealed record PageRealtimeEnvelope(
    string EventName,
    Guid WorkspaceId,
    Guid PageId,
    Guid? ParentPageId,
    Guid ActorId,
    DateTimeOffset OccurredAtUtc,
    long? Revision,
    object? Payload);

public sealed record PageUserRealtimeEnvelope(
    string EventName,
    Guid UserId,
    Guid WorkspaceId,
    Guid PageId,
    Guid? ParentPageId,
    Guid ActorId,
    DateTimeOffset OccurredAtUtc,
    long? Revision,
    object? Payload);
