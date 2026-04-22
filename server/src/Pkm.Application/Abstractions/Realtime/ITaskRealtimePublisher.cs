namespace Pkm.Application.Abstractions.Realtime;

public interface ITaskRealtimePublisher
{
    Task PublishToWorkspaceAsync(
        TaskRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);

    Task PublishToPageAsync(
        TaskRealtimeEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public sealed record TaskRealtimeEnvelope(
    string EventName,
    Guid WorkspaceId,
    Guid? PageId,
    Guid TaskId,
    Guid ActorId,
    DateTimeOffset OccurredAtUtc,
    object? Payload);