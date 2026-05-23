using Pkm.Api.Contracts.Responses.Social;

namespace Pkm.Api.Contracts.Responses.Messaging;

public sealed record ConversationResponse(
    Guid Id,
    UserSummaryResponse OtherUser,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAtUtc,
    int UnreadCount,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record MessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    Guid RecipientUserId,
    string Type,
    string? Body,
    string? ImageUrl,
    Guid? AttachmentFileId,
    bool IsMine,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedDate,
    DateTimeOffset? UpdatedDate);

public sealed record ConversationPagedResultResponse(
    IReadOnlyList<ConversationResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record MessagePagedResultResponse(
    IReadOnlyList<MessageResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);
