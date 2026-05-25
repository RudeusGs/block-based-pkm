using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Messaging;

public sealed class MessagePin : EntityBase
{
    public Guid ConversationId { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }

    private MessagePin()
    {
    }

    private MessagePin(Guid id, Guid conversationId, Guid messageId, Guid userId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(conversationId, nameof(conversationId));
        DomainGuard.AgainstEmpty(messageId, nameof(messageId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        ConversationId = conversationId;
        MessageId = messageId;
        UserId = userId;
    }

    public static MessagePin Create(Guid id, Guid conversationId, Guid messageId, Guid userId, DateTimeOffset now)
        => new(id, conversationId, messageId, userId, now);
}
