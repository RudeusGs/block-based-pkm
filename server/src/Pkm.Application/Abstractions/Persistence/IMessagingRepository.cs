using Pkm.Application.Features.Messaging.Models;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Abstractions.Persistence;

public interface IMessagingRepository
{
    Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationForParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationForUpdateAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    Task<ConversationDto?> GetConversationDtoAsync(Guid conversationId, Guid viewerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationDto>> ListConversationsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountConversationsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> ListMessagesAsync(Guid conversationId, Guid viewerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<MessageDto?> GetMessageDtoAsync(Guid messageId, Guid viewerUserId, CancellationToken cancellationToken = default);

    Task<Message?> GetMessageForRecipientAsync(Guid messageId, Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<int> CountMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);

    Task<int> MarkConversationReadAsync(Guid conversationId, Guid readerUserId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default);

    void AddConversation(Conversation conversation);

    void AddMessage(Message message);

    void UpdateConversation(Conversation conversation);
}
