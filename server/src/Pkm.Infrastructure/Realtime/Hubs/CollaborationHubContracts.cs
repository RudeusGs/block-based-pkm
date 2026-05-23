using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Features.Documents.Models;

namespace Pkm.Infrastructure.Realtime.Hubs;

public sealed record WorkspaceJoinAck(
    Guid WorkspaceId,
    string GroupName,
    WorkspacePresenceDto Presence);

public sealed record ConversationJoinAck(
    Guid ConversationId,
    string GroupName);

public sealed record PageJoinAck(
    Guid WorkspaceId,
    Guid PageId,
    string GroupName,
    PagePresenceDto Presence);

public sealed record PageCursorRequest(
    Guid PageId,
    Guid? BlockId,
    string? AnchorKey,
    int? Offset,
    string? Color);

public sealed record PageMousePointerRequest(
    Guid PageId,
    Guid? BlockId,
    double X,
    double Y,
    string? Color,
    bool IsLeaving = false);

public sealed record BlockDraftRequest(
    Guid PageId,
    Guid BlockId,
    string EditorSessionId,
    long BaseRevision,
    long ClientSequence,
    string? Type,
    string? TextContent,
    string? PropsJson);

public sealed record BlockEditingStateRequest(
    Guid PageId,
    Guid BlockId,
    string EditorSessionId,
    bool IsEditing);

