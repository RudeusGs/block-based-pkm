using Pkm.Domain.Common;

namespace Pkm.Domain.Tasks;

public sealed class TaskComment : EntityBase
{
    private const int MaxContentLength = 2000;
    private const string DeletedMessage = "Bình luận này đã bị gỡ bỏ.";

    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ParentId { get; private set; }

    public string Content { get; private set; } = string.Empty;
    public string? OriginalContent { get; private set; }

    private TaskComment() { }

    private TaskComment(
        Guid id,
        Guid taskId,
        Guid userId,
        string content,
        DateTimeOffset now,
        Guid? parentId = null) : base(id, now)
    {
        DomainGuard.AgainstEmpty(taskId, nameof(taskId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        TaskId = taskId;
        UserId = userId;
        ParentId = parentId;
        Content = NormalizeContent(content);
    }

    public static TaskComment Create(
        Guid taskId,
        Guid userId,
        string content,
        DateTimeOffset now)
    {
        return new TaskComment(
            Guid.NewGuid(),
            taskId,
            userId,
            content,
            now);
    }

    public static TaskComment CreateReply(
        Guid taskId,
        Guid userId,
        string content,
        Guid parentId,
        DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(parentId, nameof(parentId));

        return new TaskComment(
            Guid.NewGuid(),
            taskId,
            userId,
            content,
            now,
            parentId);
    }

    public void UpdateContent(string newContent, Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureOwner(actorId);

        Content = NormalizeContent(newContent);
        Touch(now);
    }

    public void DeleteByOwner(Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureOwner(actorId);

        if (OriginalContent is null)
            OriginalContent = Content;

        Content = DeletedMessage;
        SoftDelete(now);
    }

    public void RestoreByOwner(Guid actorId, DateTimeOffset now)
    {
        if (!IsDeleted)
            return;

        EnsureOwner(actorId);

        Restore(now);

        if (!string.IsNullOrWhiteSpace(OriginalContent))
            Content = OriginalContent;

        OriginalContent = null;
    }

    public void ModerateDelete(Guid actorId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        if (IsDeleted)
            return;

        if (OriginalContent is null)
            OriginalContent = Content;

        Content = DeletedMessage;
        SoftDelete(now);
    }

    private void EnsureOwner(Guid actorId)
    {
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        if (UserId != actorId)
            throw new DomainException("Không có quyền thao tác.");
    }

    private static string NormalizeContent(string content)
    {
        return TextRules.NormalizeRequired(content, MaxContentLength, "Nội dung");
    }
}