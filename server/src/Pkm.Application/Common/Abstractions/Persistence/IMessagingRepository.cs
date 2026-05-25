using Pkm.Application.Features.Messaging.Models;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IMessagingRepository : IMessagingReadRepository, IMessagingWriteRepository
{
}

public interface IMessagingReadRepository
{
    Task<Conversation?> GetDirectConversationAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default);

    Task<Conversation?> GetConversationForParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    Task<ConversationDto?> GetConversationDtoAsync(Guid conversationId, Guid viewerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConversationDto>> ListConversationsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountConversationsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> ListMessagesAsync(Guid conversationId, Guid viewerUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<MessageDto?> GetMessageDtoAsync(Guid messageId, Guid viewerUserId, CancellationToken cancellationToken = default);

    Task<Message?> GetMessageForRecipientAsync(Guid messageId, Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<int> CountMessagesAsync(Guid conversationId, CancellationToken cancellationToken = default);
}

public interface IMessagingWriteRepository
{
    Task<Conversation?> GetConversationForUpdateAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

    Task<Message?> GetMessageForParticipantForUpdateAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);

    Task<int> MarkConversationReadAsync(Guid conversationId, Guid readerUserId, DateTimeOffset readAtUtc, CancellationToken cancellationToken = default);

    Task<MessageReaction?> GetReactionForUserForUpdateAsync(Guid messageId, Guid userId, CancellationToken cancellationToken = default);

    Task<MessagePin?> GetPinForMessageForUpdateAsync(Guid messageId, CancellationToken cancellationToken = default);

    void AddConversation(Conversation conversation);

    void AddMessage(Message message);

    void AddReaction(MessageReaction reaction);

    void AddPin(MessagePin pin);

    void RemoveReaction(MessageReaction reaction);

    void RemovePin(MessagePin pin);

    void UpdateConversation(Conversation conversation);

    void UpdateMessage(Message message);

    void UpdateReaction(MessageReaction reaction);
}
