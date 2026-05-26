using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Messaging;

public sealed class Message : EntityBase
{
    public const int MaxBodyLength = 4000;
    public const int MaxUrlLength = 2000;

    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public MessageType Type { get; private set; }
    public string? Body { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? AttachmentFileId { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }
    public bool IsDeletedForEveryone { get; private set; }

    private Message()
    {
    }

    private Message(
        Guid id,
        Guid conversationId,
        Guid senderUserId,
        Guid recipientUserId,
        MessageType type,
        string? body,
        string? imageUrl,
        Guid? attachmentFileId,
        DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(conversationId, nameof(conversationId));
        DomainGuard.AgainstEmpty(senderUserId, nameof(senderUserId));
        DomainGuard.AgainstEmpty(recipientUserId, nameof(recipientUserId));

        if (senderUserId == recipientUserId)
            throw new DomainException("Message sender and recipient must be different users.");

        ConversationId = conversationId;
        SenderUserId = senderUserId;
        RecipientUserId = recipientUserId;
        Type = type;
        Body = TextRules.NormalizeOptional(body, MaxBodyLength, nameof(Body));
        ImageUrl = TextRules.NormalizeOptional(imageUrl, MaxUrlLength, nameof(ImageUrl));
        AttachmentFileId = attachmentFileId;

        if (Type == MessageType.Text && string.IsNullOrWhiteSpace(Body))
            throw new DomainException("Text message body is required.");

        if (Type == MessageType.Image && string.IsNullOrWhiteSpace(ImageUrl))
            throw new DomainException("Image message URL is required.");

        if (Type == MessageType.WorkspaceShare && string.IsNullOrWhiteSpace(Body))
            throw new DomainException("Workspace share message payload is required.");
    }

    public static Message CreateText(
        Guid id,
        Guid conversationId,
        Guid senderUserId,
        Guid recipientUserId,
        string body,
        DateTimeOffset now)
        => new(id, conversationId, senderUserId, recipientUserId, MessageType.Text, body, null, null, now);

    public static Message CreateImage(
        Guid id,
        Guid conversationId,
        Guid senderUserId,
        Guid recipientUserId,
        string? caption,
        string imageUrl,
        Guid? attachmentFileId,
        DateTimeOffset now)
        => new(id, conversationId, senderUserId, recipientUserId, MessageType.Image, caption, imageUrl, attachmentFileId, now);

    public static Message CreateWorkspaceShare(
        Guid id,
        Guid conversationId,
        Guid senderUserId,
        Guid recipientUserId,
        string payloadJson,
        DateTimeOffset now)
        => new(id, conversationId, senderUserId, recipientUserId, MessageType.WorkspaceShare, payloadJson, null, null, now);

    public string BuildPreview()
        => Type switch
        {
            MessageType.Image => string.IsNullOrWhiteSpace(Body) ? "Sent an image" : Body!,
            MessageType.WorkspaceShare => "Shared a workspace",
            _ => Body ?? string.Empty
        };

    public void MarkRead(DateTimeOffset now)
    {
        if (ReadAtUtc.HasValue)
            return;

        ReadAtUtc = now;
        Touch(now);
    }

    public void DeleteForEveryone(DateTimeOffset now)
    {
        ThrowIfDeleted();
        IsDeletedForEveryone = true;
        Body = null;
        ImageUrl = null;
        AttachmentFileId = null;
        Touch(now);
    }
}
