using Pkm.Domain.Common;

namespace Pkm.Domain.Notifications;

public sealed class Notification : EntityBase
{
    private const int MaxTitleLength = 200;
    private const int MaxMessageLength = 2000;
    private const int MaxReferenceTypeLength = 100;

    public Guid UserId { get; private set; }
    public Guid? WorkspaceId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;

    public Guid? ReferenceId { get; private set; }
    public string? ReferenceType { get; private set; }

    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    private Notification() { }

    public Notification(
        Guid id,
        Guid userId,
        Guid? workspaceId,
        NotificationType type,
        string title,
        string message,
        DateTimeOffset now,
        Guid? referenceId = null,
        string? referenceType = null) : base(id, now)
    {
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        UserId = userId;
        WorkspaceId = workspaceId;
        Type = type;

        Title = TextRules.NormalizeRequired(title, MaxTitleLength, nameof(Title));
        Message = TextRules.NormalizeRequired(message, MaxMessageLength, nameof(Message));

        ReferenceId = referenceId;
        ReferenceType = TextRules.NormalizeOptional(referenceType, MaxReferenceTypeLength, nameof(ReferenceType));

        IsRead = false;
    }

    public void MarkAsRead(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (IsRead) return;

        IsRead = true;
        ReadAtUtc = now;

        Touch(now);
    }

    public void MarkAsUnread(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (!IsRead) return;

        IsRead = false;
        ReadAtUtc = null;

        Touch(now);
    }
}