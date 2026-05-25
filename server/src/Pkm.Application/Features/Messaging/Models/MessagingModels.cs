using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Features.Messaging.Models;

public sealed record ConversationDto(
    Guid Id,
    UserSummaryDto OtherUser,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);


public sealed record WorkspaceSharePayloadDto(
    Guid WorkspaceId,
    string WorkspaceName,
    string? WorkspaceDescription,
    string WorkspaceVisibility,
    string GrantedRole,
    Guid SharedByUserId,
    string SharedByDisplayName,
    DateTimeOffset SharedAtUtc);

public sealed record MessageReactionDto(
    string Emoji,
    int Count,
    bool ReactedByMe);

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid RecipientUserId,
    MessageType Type,
    string? Body,
    string? ImageUrl,
    Guid? AttachmentFileId,
    bool IsMine,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate,
    IReadOnlyList<MessageReactionDto> Reactions,
    bool IsPinned);

public sealed record ConversationPagedResultDto(
    IReadOnlyList<ConversationDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record MessagePagedResultDto(
    IReadOnlyList<MessageDto> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
