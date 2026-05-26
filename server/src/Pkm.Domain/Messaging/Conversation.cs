using Pkm.Domain.SharedKernel;
using Pkm.Domain.Social;

namespace Pkm.Domain.Messaging;

public sealed class Conversation : EntityBase
{
    public const int MaxPreviewLength = 500;

    public Guid FirstUserId { get; private set; }
    public Guid SecondUserId { get; private set; }
    public DateTimeOffset? LastMessageAtUtc { get; private set; }
    public string? LastMessagePreview { get; private set; }

    private Conversation()
    {
    }

    private Conversation(Guid id, Guid firstUserId, Guid secondUserId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(firstUserId, nameof(firstUserId));
        DomainGuard.AgainstEmpty(secondUserId, nameof(secondUserId));

        if (firstUserId == secondUserId)
            throw new DomainException("A direct conversation must contain two different users.");

        FirstUserId = firstUserId;
        SecondUserId = secondUserId;
    }

    public static Conversation CreateDirect(Guid id, Guid userAId, Guid userBId, DateTimeOffset now)
    {
        var ordered = Friendship.OrderPair(userAId, userBId);
        return new Conversation(id, ordered.First, ordered.Second, now);
    }

    public bool HasParticipant(Guid userId) => FirstUserId == userId || SecondUserId == userId;

    public Guid GetOtherParticipant(Guid userId)
    {
        if (FirstUserId == userId)
            return SecondUserId;

        if (SecondUserId == userId)
            return FirstUserId;

        throw new DomainException("The user is not a participant in this conversation.");
    }

    public void RegisterMessage(string preview, DateTimeOffset sentAtUtc)
    {
        ThrowIfDeleted();
        LastMessagePreview = TextRules.NormalizeOptional(preview, MaxPreviewLength, nameof(LastMessagePreview));
        LastMessageAtUtc = sentAtUtc;
        Touch(sentAtUtc);
    }
}
