using Pkm.Domain.Common;

namespace Pkm.Domain.Social;

public sealed class Friendship : EntityBase
{
    public Guid FirstUserId { get; private set; }
    public Guid SecondUserId { get; private set; }

    private Friendship()
    {
    }

    private Friendship(Guid id, Guid firstUserId, Guid secondUserId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(firstUserId, nameof(firstUserId));
        DomainGuard.AgainstEmpty(secondUserId, nameof(secondUserId));

        if (firstUserId == secondUserId)
            throw new DomainException("Một quan hệ bạn bè phải gồm 2 người khác nhau.");

        FirstUserId = firstUserId;
        SecondUserId = secondUserId;
    }

    public static Friendship Create(Guid id, Guid userAId, Guid userBId, DateTimeOffset now)
    {
        var ordered = OrderPair(userAId, userBId);
        return new Friendship(id, ordered.First, ordered.Second, now);
    }

    public static (Guid First, Guid Second) OrderPair(Guid userAId, Guid userBId)
        => string.CompareOrdinal(userAId.ToString("N"), userBId.ToString("N")) <= 0
            ? (userAId, userBId)
            : (userBId, userAId);

    public bool Connects(Guid userAId, Guid userBId)
    {
        var ordered = OrderPair(userAId, userBId);
        return FirstUserId == ordered.First && SecondUserId == ordered.Second;
    }
}
