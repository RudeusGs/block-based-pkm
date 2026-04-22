using Pkm.Domain.Common;

namespace Pkm.Domain.Pages;

/// <summary>
/// PageRevision dùng để version hóa document (Page + Block tree)
/// Mỗi lần thay đổi nội dung thì RevisionNumber sẽ tăng lên 1, và lưu lại ai là người thay đổi, khi nào thay đổi, và lý do (nếu có).
/// </summary>
public sealed class PageRevision : CreationAuditedEntity
{
    private const int MaxReasonLength = 500;

    public Guid PageId { get; private set; }
    public long RevisionNumber { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string? Reason { get; private set; }

    private PageRevision() { }

    private PageRevision(
        Guid id,
        Guid pageId,
        long revisionNumber,
        Guid createdBy,
        DateTimeOffset createdDate,
        string? reason = null) : base(id, createdDate)
    {
        DomainGuard.AgainstEmpty(pageId, nameof(pageId));
        DomainGuard.AgainstNonPositive(revisionNumber, nameof(revisionNumber));
        DomainGuard.AgainstEmpty(createdBy, nameof(createdBy));

        PageId = pageId;
        RevisionNumber = revisionNumber;
        CreatedBy = createdBy;
        Reason = TextRules.NormalizeOptional(reason, MaxReasonLength, nameof(Reason));
    }

    public static PageRevision Create(
        Guid pageId,
        long revisionNumber,
        Guid createdBy,
        DateTimeOffset now,
        string? reason = null)
    {
        return new PageRevision(
            Guid.NewGuid(),
            pageId,
            revisionNumber,
            createdBy,
            now,
            reason);
    }
}