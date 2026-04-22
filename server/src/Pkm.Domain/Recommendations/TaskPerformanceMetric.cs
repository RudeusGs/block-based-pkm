using Pkm.Domain.Common;

namespace Pkm.Domain.Recommendations;

/// <summary>
/// TaskPerformanceMetric: source of truth cho dữ liệu tracking cơ bản của Task theo từng User.
/// </summary>
public sealed class TaskPerformanceMetric : EntityBase
{
    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid WorkspaceId { get; private set; }

    public int CompletionCount { get; private set; }
    public int AbandonedCount { get; private set; }

    public DateTimeOffset? LastCompletedAt { get; private set; }

    private TaskPerformanceMetric() { }

    public TaskPerformanceMetric(
        Guid id,
        Guid taskId,
        Guid userId,
        Guid workspaceId,
        DateTimeOffset now) : base(id, now)
    {
        DomainGuard.AgainstEmpty(taskId, nameof(taskId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));

        TaskId = taskId;
        UserId = userId;
        WorkspaceId = workspaceId;
    }

    public void RecordCompletion(DateTimeOffset completedAt, DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (completedAt > now)
            throw new DomainException("Completion time không hợp lệ.");

        CompletionCount++;
        LastCompletedAt = completedAt;

        Touch(now);
    }

    public void RecordAbandonment(DateTimeOffset now)
    {
        ThrowIfDeleted();

        AbandonedCount++;
        Touch(now);
    }
}