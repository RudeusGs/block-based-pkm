using Pkm.Domain.SharedKernel;

namespace Pkm.Domain.Social;

public sealed class FriendRequest : EntityBase
{
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public FriendRequestStatus Status { get; private set; }
    public DateTimeOffset? RespondedAtUtc { get; private set; }

    private FriendRequest()
    {
    }

    private FriendRequest(Guid id, Guid requesterId, Guid addresseeId, DateTimeOffset now)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(requesterId, nameof(requesterId));
        DomainGuard.AgainstEmpty(addresseeId, nameof(addresseeId));

        if (requesterId == addresseeId)
            throw new DomainException("A user cannot send a friend request to themselves.");

        RequesterId = requesterId;
        AddresseeId = addresseeId;
        Status = FriendRequestStatus.Pending;
    }

    public static FriendRequest Create(Guid id, Guid requesterId, Guid addresseeId, DateTimeOffset now)
        => new(id, requesterId, addresseeId, now);

    public bool IsPending => Status == FriendRequestStatus.Pending;

    public void Accept(DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsurePending();
        Status = FriendRequestStatus.Accepted;
        RespondedAtUtc = now;
        Touch(now);
    }

    public void Reject(DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsurePending();
        Status = FriendRequestStatus.Rejected;
        RespondedAtUtc = now;
        Touch(now);
    }

    public void Cancel(DateTimeOffset now)
    {
        ThrowIfDeleted();
        EnsurePending();
        Status = FriendRequestStatus.Cancelled;
        RespondedAtUtc = now;
        Touch(now);
    }

    private void EnsurePending()
    {
        if (Status != FriendRequestStatus.Pending)
            throw new DomainException("The friend request is no longer pending.");
    }
}
