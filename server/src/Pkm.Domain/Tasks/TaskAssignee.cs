using Pkm.Domain.Common;

namespace Pkm.Domain.Tasks;

/// <summary>
/// TaskAssignee: thể hiện một người được giao vào một task.
/// </summary>
public sealed class TaskAssignee : CreationAuditedEntity
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }

    private TaskAssignee() { }

    private TaskAssignee(Guid id, Guid taskId, Guid userId, DateTimeOffset assignedAt)
        : base(id, assignedAt)
    {
        DomainGuard.AgainstEmpty(taskId, nameof(taskId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        TaskId = taskId;
        UserId = userId;
    }

    public static TaskAssignee Create(Guid taskId, Guid userId, DateTimeOffset now)
    {
        return new TaskAssignee(
            Guid.NewGuid(),
            taskId,
            userId,
            now);
    }
}