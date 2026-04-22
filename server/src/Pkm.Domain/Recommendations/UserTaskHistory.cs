using Pkm.Domain.Common;

namespace Pkm.Domain.Recommendations;

/// <summary>
/// UserTaskHistory: ghi nhận một session tương tác của User với Task.
/// Flow hợp lệ: InProgress -> Completed / Abandoned / Skipped.
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
            throw new DomainException("Thời gian không hợp lệ.");

        DurationMinutes = diff.TotalMinutes < 1
            ? 1
            : (int)Math.Round(diff.TotalMinutes);
    }

    private void EnsureInProgress()
    {
        ThrowIfDeleted();

        if (Status != StatusUserTaskHistory.InProgress)
            throw new DomainException("Chỉ có thể thao tác khi đang InProgress.");
    }
}