namespace Pkm.Application.Abstractions.Realtime;

public sealed record PagePresenceEntry(
    Guid PageId,
    Guid WorkspaceId,
    Guid UserId,
    string? UserName,
    string ConnectionId,
    DateTimeOffset LastSeenUtc);

public sealed record BlockLeaseInfo(
    Guid BlockId,
    Guid PageId,
    Guid UserId,
    string ConnectionId,
    DateTimeOffset AcquiredAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? HolderDisplayName = null);

public sealed record BlockLeaseResult(
    bool Granted,
    BlockLeaseInfo? Lease,
    BlockLeaseInfo? CurrentHolder);

public sealed record DocumentRealtimeEnvelope(
    string EventName,
    Guid WorkspaceId,
    Guid? PageId,
    Guid? BlockId,
    Guid ActorId,
    DateTimeOffset OccurredAtUtc,
    long? Revision,
    object? Payload);