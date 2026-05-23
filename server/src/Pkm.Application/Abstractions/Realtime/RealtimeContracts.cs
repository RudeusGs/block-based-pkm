namespace Pkm.Application.Abstractions.Realtime;

public sealed record WorkspacePresenceEntry(
    Guid WorkspaceId,
    Guid UserId,
    string? UserName,
    string ConnectionId,
    DateTimeOffset LastSeenUtc);

public sealed record WorkspacePresenceUserDto(
    Guid UserId,
    string? UserName,
    int ConnectionCount,
    DateTimeOffset LastSeenUtc);

public sealed record WorkspacePresenceDto(
    Guid WorkspaceId,
    IReadOnlyList<WorkspacePresenceUserDto> ActiveUsers);

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

public sealed record PageCursorDto(
    Guid WorkspaceId,
    Guid PageId,
    Guid? BlockId,
    Guid UserId,
    string? UserName,
    string ConnectionId,
    string? AnchorKey,
    int? Offset,
    string? Color,
    DateTimeOffset OccurredAtUtc);

public sealed record PageMousePointerDto(
    Guid WorkspaceId,
    Guid PageId,
    Guid? BlockId,
    Guid UserId,
    string? UserName,
    string ConnectionId,
    double X,
    double Y,
    string? Color,
    bool IsLeaving,
    DateTimeOffset OccurredAtUtc);

public sealed record BlockDraftDto(
    Guid WorkspaceId,
    Guid PageId,
    Guid BlockId,
    Guid UserId,
    string? UserName,
    string ConnectionId,
    string EditorSessionId,
    long BaseRevision,
    long ClientSequence,
    string? Type,
    string? TextContent,
    string? PropsJson,
    DateTimeOffset OccurredAtUtc);

public sealed record BlockEditingStateDto(
    Guid WorkspaceId,
    Guid PageId,
    Guid BlockId,
    Guid UserId,
    string? UserName,
    string ConnectionId,
    string EditorSessionId,
    bool IsEditing,
    DateTimeOffset OccurredAtUtc);

public sealed record SocialRealtimeEnvelope(
    string EventName,
    Guid UserId,
    Guid? ActorId,
    DateTimeOffset OccurredAtUtc,
    object? Payload);

public sealed record MessagingRealtimeEnvelope(
    string EventName,
    Guid ConversationId,
    Guid SenderUserId,
    Guid RecipientUserId,
    DateTimeOffset OccurredAtUtc,
    object? Payload);
