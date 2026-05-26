using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Messaging;

public sealed class MessageReaction : EntityBase
{
    public const int MaxEmojiLength = 32;

    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Emoji { get; private set; } = string.Empty;

    private MessageReaction()
    {
    }

    private MessageReaction(Guid id, Guid messageId, Guid userId, string emoji, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(messageId, nameof(messageId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        MessageId = messageId;
        UserId = userId;
        Emoji = NormalizeEmoji(emoji);
    }

    public static MessageReaction Create(Guid id, Guid messageId, Guid userId, string emoji, DateTimeOffset now)
        => new(id, messageId, userId, emoji, now);

    public void ChangeEmoji(string emoji, DateTimeOffset now)
    {
        ThrowIfDeleted();
        Emoji = NormalizeEmoji(emoji);
        Touch(now);
    }

    private static string NormalizeEmoji(string emoji)
    {
        var normalized = TextRules.NormalizeRequired(emoji, MaxEmojiLength, nameof(Emoji));
        if (normalized.Any(char.IsControl))
            throw new DomainException("Message reaction is invalid.");

        return normalized;
    }
}
