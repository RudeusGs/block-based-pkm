using Pkm.Domain.SharedKernel;
using Pkm.Domain.Tasks.Events;

namespace Pkm.Domain.Tasks;

/// <summary>
/// Task attached to a page inside a workspace.
/// </summary>
public sealed class WorkTask : EntityBase
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 4000;

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public StatusWorkTask Status { get; private set; }
    public PriorityWorkTask Priority { get; private set; }

    public DateTimeOffset? DueDate { get; private set; }

    public Guid WorkspaceId { get; private set; }
    public Guid? PageId { get; private set; }

    public Guid CreatedById { get; private set; }
    public Guid? LastModifiedById { get; private set; }

    private WorkTask() { }

    private WorkTask(
        Guid id,
        string title,
        Guid workspaceId,
        Guid createdById,
        DateTimeOffset now,
        Guid? pageId = null,
        PriorityWorkTask priority = PriorityWorkTask.Medium,
        string? description = null,
        DateTimeOffset? dueDate = null)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));
        DomainGuard.AgainstEmpty(createdById, nameof(createdById));

        WorkspaceId = workspaceId;
        PageId = pageId;
        CreatedById = createdById;

        Title = TextRules.NormalizeRequired(title, MaxTitleLength, nameof(Title));
        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        Priority = priority;
        DueDate = dueDate;
        Status = StatusWorkTask.ToDo;
    }

    public static WorkTask Create(
        Guid id,
        string title,
        Guid workspaceId,
        Guid createdById,
        DateTimeOffset now,
        Guid? pageId = null,
        PriorityWorkTask priority = PriorityWorkTask.Medium,
        string? description = null,
        DateTimeOffset? dueDate = null,
        IReadOnlyCollection<Guid>? assigneeUserIds = null)
    {
        var task = new WorkTask(
            id,
            title,
            workspaceId,
            createdById,
            now,
            pageId,
            priority,
            description,
            dueDate);

        task.RaiseDomainEvent(new TaskCreatedDomainEvent(
            task.Id,
            task.WorkspaceId,
            task.PageId,
            task.Title,
            task.Priority,
            task.DueDate,
            NormalizeUserIds(assigneeUserIds),
            createdById,
            now));

        return task;
    }

    public void ChangeStatus(
        StatusWorkTask newStatus,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);
        EnsureValidStatus(newStatus);

        if (Status == newStatus)
            return;

        if (!CanChangeStatusTo(newStatus))
            throw new DomainException("Completed tasks cannot be moved back to another status.");

        var oldStatus = Status;
        Status = newStatus;
        RegisterModification(actorId, now);

        RaiseDomainEvent(new TaskStatusChangedDomainEvent(
            Id,
            WorkspaceId,
            PageId,
            Title,
            oldStatus,
            newStatus,
            actorId,
            now));
    }

    public bool CanChangeStatusTo(StatusWorkTask newStatus)
    {
        if (!Enum.IsDefined(typeof(StatusWorkTask), newStatus))
            return false;

        return Status != StatusWorkTask.Done || newStatus == StatusWorkTask.Done;
    }

    public void Start(Guid actorId, DateTimeOffset now)
        => ChangeStatus(StatusWorkTask.Doing, actorId, now);

    public void Complete(Guid actorId, DateTimeOffset now)
        => ChangeStatus(StatusWorkTask.Done, actorId, now);

    public void ReOpen(Guid actorId, DateTimeOffset now)
        => ChangeStatus(StatusWorkTask.ToDo, actorId, now);

    public void UpdateDetailsAndLocation(
        string title,
        string? description,
        PriorityWorkTask priority,
        DateTimeOffset? dueDate,
        Guid workspaceId,
        Guid? pageId,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));

        var oldTitle = Title;
        var oldDescription = Description;
        var oldPriority = Priority;
        var oldDueDate = DueDate;
        var oldPageId = PageId;

        Title = TextRules.NormalizeRequired(title, MaxTitleLength, nameof(Title));
        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        Priority = priority;
        DueDate = dueDate;
        WorkspaceId = workspaceId;
        PageId = pageId;

        RegisterModification(actorId, now);

        RaiseDomainEvent(new TaskUpdatedDomainEvent(
            Id,
            WorkspaceId,
            oldPageId,
            PageId,
            oldTitle,
            Title,
            oldDescription,
            Description,
            oldPriority,
            Priority,
            oldDueDate,
            DueDate,
            actorId,
            now));
    }

    public void AssignTo(
        Guid assigneeUserId,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);
        DomainGuard.AgainstEmpty(assigneeUserId, nameof(assigneeUserId));

        RegisterModification(actorId, now);

        RaiseDomainEvent(new TaskAssignedDomainEvent(
            Id,
            WorkspaceId,
            PageId,
            Title,
            assigneeUserId,
            actorId,
            now));
    }

    public void UnassignFrom(
        Guid assigneeUserId,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);
        DomainGuard.AgainstEmpty(assigneeUserId, nameof(assigneeUserId));

        RegisterModification(actorId, now);

        RaiseDomainEvent(new TaskUnassignedDomainEvent(
            Id,
            WorkspaceId,
            PageId,
            Title,
            assigneeUserId,
            actorId,
            now));
    }

    public void Delete(
        Guid actorId,
        DateTimeOffset now,
        IReadOnlyCollection<Guid>? assigneeUserIds = null)
    {
        EnsureEditable(actorId);
        LastModifiedById = actorId;
        SoftDelete(now);

        RaiseDomainEvent(new TaskDeletedDomainEvent(
            Id,
            WorkspaceId,
            PageId,
            Title,
            NormalizeUserIds(assigneeUserIds),
            actorId,
            now));
    }

    private void EnsureEditable(Guid actorId)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(actorId, "ActorId");
    }

    private void RegisterModification(Guid actorId, DateTimeOffset now)
    {
        LastModifiedById = actorId;
        Touch(now);
    }

    private static void EnsureValidStatus(StatusWorkTask status)
    {
        if (!Enum.IsDefined(typeof(StatusWorkTask), status))
            throw new DomainException("Task status is invalid.");
    }

    private static Guid[] NormalizeUserIds(IReadOnlyCollection<Guid>? userIds)
    {
        return (userIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();
    }
}
