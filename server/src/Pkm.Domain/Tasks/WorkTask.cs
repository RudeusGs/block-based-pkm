using Pkm.Domain.Common;

namespace Pkm.Domain.Tasks;

/// <summary>
/// WorkTask: đại diện cho một công việc trong workspace, gắn với một page.
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
        DateTimeOffset? dueDate = null)
    {
        return new WorkTask(
            id,
            title,
            workspaceId,
            createdById,
            now,
            pageId,
            priority,
            description,
            dueDate);
    }

    public void UpdateDetails(
        string title,
        string? description,
        PriorityWorkTask priority,
        DateTimeOffset? dueDate,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);

        Title = TextRules.NormalizeRequired(title, MaxTitleLength, nameof(Title));
        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        Priority = priority;
        DueDate = dueDate;

        RegisterModification(actorId, now);
    }

    public void ChangeLocation(
        Guid workspaceId,
        Guid? pageId,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));

        WorkspaceId = workspaceId;
        PageId = pageId;

        RegisterModification(actorId, now);
    }

    public void ChangeStatus(
        StatusWorkTask newStatus,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (Status == newStatus)
            return;

        Status = newStatus;
        RegisterModification(actorId, now);
    }

    public void Start(Guid actorId, DateTimeOffset now)
        => ChangeStatus(StatusWorkTask.Doing, actorId, now);

    public void Complete(Guid actorId, DateTimeOffset now)
        => ChangeStatus(StatusWorkTask.Done, actorId, now);

    public void ReOpen(Guid actorId, DateTimeOffset now)
        => ChangeStatus(StatusWorkTask.ToDo, actorId, now);

    public void ChangePriority(
        PriorityWorkTask priority,
        Guid actorId,
        DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (Priority == priority)
            return;

        Priority = priority;
        RegisterModification(actorId, now);
    }

    public void RecordAssignmentChange(Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);
        RegisterModification(actorId, now);
    }

    public void Delete(Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);
        LastModifiedById = actorId;
        SoftDelete(now);
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
}