using Pkm.Domain.SharedKernel;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Domain.Tasks;

public sealed class TaskComment : EntityBase
{
    private const int MaxContentLength = 2000;
    private const string DeletedMessage = "This comment has been removed.";

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
        var comment = new TaskComment(
            Guid.NewGuid(),
            taskId,
            userId,
            content,
            now);

        comment.RaiseDomainEvent(new TaskCommentCreatedDomainEvent(
            comment.Id,
            comment.TaskId,
            comment.UserId,
            comment.ParentId,
            comment.Content,
            now));

        return comment;
    }

    public static TaskComment CreateReply(
        Guid taskId,
        Guid userId,
        string content,
        Guid parentId,
        DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(parentId, nameof(parentId));

        var comment = new TaskComment(
            Guid.NewGuid(),
            taskId,
            userId,
            content,
            now,
            parentId);

        comment.RaiseDomainEvent(new TaskCommentCreatedDomainEvent(
            comment.Id,
            comment.TaskId,
            comment.UserId,
            comment.ParentId,
            comment.Content,
            now));

        return comment;
    }

    public void UpdateContent(string newContent, Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureOwner(actorId);

        var oldContent = Content;
        Content = NormalizeContent(newContent);
        Touch(now);

        RaiseDomainEvent(new TaskCommentUpdatedDomainEvent(
            Id,
            TaskId,
            UserId,
            oldContent,
            Content,
            now));
    }

    public void DeleteByOwner(Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureOwner(actorId);

        if (OriginalContent is null)
            OriginalContent = Content;

        var oldContent = Content;
        Content = DeletedMessage;
        SoftDelete(now);

        RaiseDomainEvent(new TaskCommentDeletedDomainEvent(
            Id,
            TaskId,
            UserId,
            actorId,
            false,
            oldContent,
            now));
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

        RaiseDomainEvent(new TaskCommentRestoredDomainEvent(
            Id,
            TaskId,
            UserId,
            Content,
            now));
    }

    public void ModerateDelete(Guid actorId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        if (IsDeleted)
            return;

        if (OriginalContent is null)
            OriginalContent = Content;

        var oldContent = Content;
        Content = DeletedMessage;
        SoftDelete(now);

        RaiseDomainEvent(new TaskCommentDeletedDomainEvent(
            Id,
            TaskId,
            UserId,
            actorId,
            true,
            oldContent,
            now));
    }

    private void EnsureOwner(Guid actorId)
    {
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        if (UserId != actorId)
            throw new DomainException("The user is not allowed to perform this action.");
    }

    private static string NormalizeContent(string content)
    {
        return TextRules.NormalizeRequired(content, MaxContentLength, "Content");
    }
}
