using Pkm.Application.Features.Documents.Models;

namespace Pkm.Infrastructure.Realtime.Hubs;

public sealed record WorkspaceJoinAck(Guid WorkspaceId, string GroupName);

public sealed record PageJoinAck(
    Guid WorkspaceId,
    Guid PageId,
    PagePresenceDto Presence);

public sealed record BlockLeaseHubResponse(
    Guid BlockId,
    Guid PageId,
    bool Granted,
    string Status,
    Guid? HolderUserId,
    string? HolderDisplayName,
    DateTimeOffset? ExpiresAtUtc);