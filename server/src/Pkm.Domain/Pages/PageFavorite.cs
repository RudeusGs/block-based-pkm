using Pkm.Domain.Common;

namespace Pkm.Domain.Pages;

public sealed class PageFavorite : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid PageId { get; private set; }

    private PageFavorite() { }

    public PageFavorite(Guid id, Guid userId, Guid pageId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        DomainGuard.AgainstEmpty(pageId, nameof(pageId));

        UserId = userId;
        PageId = pageId;
    }
}
