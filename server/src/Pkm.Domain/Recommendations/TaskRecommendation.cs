using Pkm.Domain.Common;

namespace Pkm.Domain.Recommendations;

/// <summary>
/// TaskRecommendation: đại diện cho một recommendation được tạo bởi hệ thống.
/// Entity chỉ quản lý state transition và business rule cơ bản.
/// </summary>
public sealed class TaskRecommendation : EntityBase
{
    private const int MaxReasonLength = 1000;

    public Guid TaskId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid WorkspaceId { get; private set; }

    public decimal Score { get; private set; }
    public string? Reason { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public StatusTaskRecommendation Status { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private TaskRecommendation() { }

    public TaskRecommendation(
        Guid id,
        Guid taskId,
        Guid userId,
        Guid workspaceId,
        decimal score,
        DateTimeOffset now,
        string? reason = null,
        int validHours = 24) : base(id, now)
    {
        DomainGuard.AgainstEmpty(taskId, nameof(taskId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));

        if (score < 0 || score > 100)
            throw new DomainException("Score phải nằm trong khoảng 0-100.");

        if (validHours <= 0)
            throw new DomainException("ValidHours phải lớn hơn 0.");

        TaskId = taskId;
        UserId = userId;
        WorkspaceId = workspaceId;
        Score = score;
        Reason = TextRules.NormalizeOptional(reason, MaxReasonLength, nameof(Reason));
        ExpiresAt = now.AddHours(validHours);

        Status = StatusTaskRecommendation.Pending;
    }

    public void Accept(DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsureNotExpired(now);

        if (Status != StatusTaskRecommendation.Pending)
            throw new DomainException("Chỉ có thể accept khi đang Pending.");

        Status = StatusTaskRecommendation.Accepted;
        AcceptedAt = now;

        Touch(now);
    }

    public void Reject(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Status != StatusTaskRecommendation.Pending &&
            Status != StatusTaskRecommendation.Accepted)
        {
            throw new DomainException("Không thể reject ở trạng thái hiện tại.");
        }

        Status = StatusTaskRecommendation.Rejected;
        RejectedAt = now;

        Touch(now);
    }

    public void MarkCompleted(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Status != StatusTaskRecommendation.Accepted)
            throw new DomainException("Phải accept trước khi complete.");

        Status = StatusTaskRecommendation.Completed;
        CompletedAt = now;

        Touch(now);
    }

    public void ExpireIfNeeded(DateTimeOffset now)
    {
        ThrowIfDeleted();

        if (Status == StatusTaskRecommendation.Pending &&
            ExpiresAt.HasValue &&
            now > ExpiresAt.Value)
        {
            Status = StatusTaskRecommendation.Expired;
            Touch(now);
        }
    }

    private void EnsureNotExpired(DateTimeOffset now)
    {
        if (ExpiresAt.HasValue && now > ExpiresAt.Value)
        {
            if (Status == StatusTaskRecommendation.Pending)
            {
                Status = StatusTaskRecommendation.Expired;
                Touch(now);
            }

            throw new DomainException("Recommendation đã hết hạn.");
        }

        if (Status == StatusTaskRecommendation.Expired)
            throw new DomainException("Recommendation đã hết hạn.");
    }
}