using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Recommendations;

/// <summary>
/// Records one user interaction session with a task.
/// Valid flow: InProgress -> Completed / Abandoned / Skipped.
/// </summary>
public sealed class UserTaskHistory : EntityBase
{
    private const int MaxNotesLength = 1000;

    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }
    public int DurationMinutes { get; private set; }

    public StatusUserTaskHistory Status { get; private set; }
    public string? Notes { get; private set; }

    private UserTaskHistory() { }

    public UserTaskHistory(Guid id, Guid taskId, Guid userId, DateTimeOffset startedAt)
        : base(id, startedAt)
    {
        DomainGuard.AgainstEmpty(taskId, nameof(taskId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));

        TaskId = taskId;
        UserId = userId;
        Status = StatusUserTaskHistory.InProgress;
    }

    public void MarkAsCompleted(DateTimeOffset now, string? notes = null)
    {
        EnsureInProgress();

        CompletedAt = now;
        Status = StatusUserTaskHistory.Completed;
        Notes = TextRules.NormalizeOptional(notes, MaxNotesLength, nameof(Notes));

        CalculateDuration();
        Touch(now);
    }

    public void MarkAsAbandoned(DateTimeOffset now, string? notes = null)
    {
        EnsureInProgress();

        CompletedAt = now;
        Status = StatusUserTaskHistory.Abandoned;
        Notes = TextRules.NormalizeOptional(notes, MaxNotesLength, nameof(Notes));

        CalculateDuration();
        Touch(now);
    }

    public void MarkAsSkipped(DateTimeOffset now)
    {
        EnsureInProgress();

        CompletedAt = now;
        Status = StatusUserTaskHistory.Skipped;
        DurationMinutes = 0;

        Touch(now);
    }

    private void CalculateDuration()
    {
        if (!CompletedAt.HasValue)
            return;

        var diff = CompletedAt.Value - CreatedDate;

        if (diff.TotalMinutes < 0)
            throw new DomainException("Task history timestamp is invalid.");

        DurationMinutes = diff.TotalMinutes < 1
            ? 1
            : (int)Math.Round(diff.TotalMinutes);
    }

    private void EnsureInProgress()
    {
        ThrowIfDeleted();

        if (Status != StatusUserTaskHistory.InProgress)
            throw new DomainException("Task history can only be changed while it is in progress.");
    }
}
