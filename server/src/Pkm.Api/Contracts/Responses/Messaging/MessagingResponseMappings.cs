using Pkm.Api.Contracts.Responses.Social;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Api.Contracts.Responses.Messaging;

public static class MessagingResponseMappings
{
    public static ConversationResponse ToResponse(this ConversationDto dto)
        => new(
            dto.Id,
            dto.OtherUser.ToResponse(),
            dto.LastMessagePreview,
            dto.LastMessageAtUtc,
            dto.UnreadCount,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static MessageReactionResponse ToResponse(this MessageReactionDto dto)
        => new(
            dto.Emoji,
            dto.Count,
            dto.ReactedByMe);

    public static MessageResponse ToResponse(this MessageDto dto)
        => new(
            dto.Id,
            dto.ConversationId,
            dto.SenderUserId,
            dto.RecipientUserId,
            dto.Type.ToString(),
            dto.Body,
            dto.ImageUrl,
            dto.AttachmentFileId,
            dto.IsMine,
            dto.ReadAtUtc,
            dto.CreatedDate,
            dto.UpdatedDate,
            dto.Reactions.Select(x => x.ToResponse()).ToArray(),
            dto.IsPinned);

    public static ConversationPagedResultResponse ToResponse(this ConversationPagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);

    public static MessagePagedResultResponse ToResponse(this MessagePagedResultDto dto)
        => new(
            dto.Items.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);
}
