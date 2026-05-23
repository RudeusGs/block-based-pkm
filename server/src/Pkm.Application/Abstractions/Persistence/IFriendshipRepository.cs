using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Social;

namespace Pkm.Application.Abstractions.Persistence;

public interface IFriendshipRepository
{
    Task<bool> AreFriendsAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default);

    Task<Friendship?> GetFriendshipAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default);

    Task<FriendRequest?> GetPendingRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

    Task<FriendRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(Guid viewerUserId, string keyword, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FriendDto>> ListFriendsAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountFriendsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FriendRequestDto>> ListIncomingRequestsAsync(Guid addresseeId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FriendRequestDto>> ListOutgoingRequestsAsync(Guid requesterId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    void AddRequest(FriendRequest request);

    void AddFriendship(Friendship friendship);

    void UpdateRequest(FriendRequest request);

    void RemoveFriendship(Friendship friendship);
}
