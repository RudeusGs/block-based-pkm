using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Pages;

/// <summary>
/// Version record for a document, including the page metadata and block tree.
/// Each content change increments RevisionNumber and captures actor, timestamp, and optional reason.
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
