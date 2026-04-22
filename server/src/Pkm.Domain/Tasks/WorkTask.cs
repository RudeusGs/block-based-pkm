using Pkm.Domain.Common;

namespace Pkm.Domain.Tasks;

/// <summary>
/// WorkTask: đại diện cho một công việc trong hệ thống.
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

    public WorkTask(
        Guid id,
        string title,
        Guid workspaceId,
        Guid createdById,
        DateTimeOffset now,
        Guid? pageId = null,
        PriorityWorkTask priority = PriorityWorkTask.Medium,
        string? description = null,
        DateTimeOffset? dueDate = null) : base(id, now)
    {
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));
        DomainGuard.AgainstEmpty(createdById, nameof(createdById));

        WorkspaceId = workspaceId;
        CreatedById = createdById;
        PageId = pageId;

        Title = TextRules.NormalizeRequired(title, MaxTitleLength, nameof(Title));
        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        Priority = priority;
        DueDate = dueDate;
        Status = StatusWorkTask.ToDo;
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

    public void ChangeLocation(Guid workspaceId, Guid? pageId, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));

        WorkspaceId = workspaceId;
        PageId = pageId;

        RegisterModification(actorId, now);
    }

    public void Complete(Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (Status == StatusWorkTask.Done) return;

        Status = StatusWorkTask.Done;
        RegisterModification(actorId, now);
    }

    public void ReOpen(Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (Status == StatusWorkTask.ToDo) return;

        Status = StatusWorkTask.ToDo;
        RegisterModification(actorId, now);
    }

    public void ChangePriority(PriorityWorkTask priority, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (Priority == priority) return;

        Priority = priority;
        RegisterModification(actorId, now);
    }

    public void Reschedule(DateTimeOffset? dueDate, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        DueDate = dueDate;
        RegisterModification(actorId, now);
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