using Pkm.Domain.Common;

namespace Pkm.Domain.Profiles;

public sealed class UserProfilePage : EntityBase
{
    public const int MaxBioLength = 1000;
    public const int MaxUrlLength = 2000;

    public Guid UserId { get; private set; }
    public string? Bio { get; private set; }
    public string? CoverImageUrl { get; private set; }

    private UserProfilePage()
    {
    }

    private UserProfilePage(Guid id, Guid userId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        UserId = userId;
    }

    public static UserProfilePage Create(Guid id, Guid userId, DateTimeOffset now)
        => new(id, userId, now);

    public void Update(string? bio, string? coverImageUrl, DateTimeOffset now)
    {
        ThrowIfDeleted();
        Bio = TextRules.NormalizeOptional(bio, MaxBioLength, nameof(Bio));
        CoverImageUrl = TextRules.NormalizeOptional(coverImageUrl, MaxUrlLength, nameof(CoverImageUrl));
        Touch(now);
    }

    public void SetCoverImage(string coverImageUrl, DateTimeOffset now)
    {
        ThrowIfDeleted();
        CoverImageUrl = TextRules.NormalizeOptional(coverImageUrl, MaxUrlLength, nameof(CoverImageUrl));
        Touch(now);
    }
}
