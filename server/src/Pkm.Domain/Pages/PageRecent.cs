using Pkm.Domain.Common;

namespace Pkm.Domain.Pages;

public sealed class PageRecent : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid PageId { get; private set; }
    public DateTimeOffset LastVisitedAtUtc { get; private set; }
    public int VisitCount { get; private set; }

    private PageRecent() { }

    public PageRecent(Guid id, Guid userId, Guid pageId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        DomainGuard.AgainstEmpty(pageId, nameof(pageId));

        UserId = userId;
        PageId = pageId;
        LastVisitedAtUtc = now;
        VisitCount = 1;
    }

    public void MarkVisited(DateTimeOffset now)
    {
        ThrowIfDeleted();
        LastVisitedAtUtc = now;
        VisitCount++;
        Touch(now);
    }
}
