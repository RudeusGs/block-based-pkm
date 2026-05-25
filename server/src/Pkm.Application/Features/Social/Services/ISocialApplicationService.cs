using Pkm.Application.Common.Results;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Services;

public interface ISocialApplicationService : ISocialCommandService, ISocialQueryService
{
}

public interface ISocialCommandService
{
    Task<Result<FriendRequestDto>> SendFriendRequestAsync(Guid addresseeUserId, CancellationToken cancellationToken = default);

    Task<Result<FriendRequestDto>> AcceptFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<Result<FriendRequestDto>> RejectFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<Result<FriendRequestDto>> CancelFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<Result> RemoveFriendAsync(Guid friendUserId, CancellationToken cancellationToken = default);

    Task<Result<UserProfilePageDto>> UpdateMyProfilePageAsync(string? bio, string? coverImageUrl, CancellationToken cancellationToken = default);

    Task<Result<UserProfilePageDto>> UploadMyProfileCoverImageAsync(string fileName, string contentType, long sizeBytes, Stream content, CancellationToken cancellationToken = default);
}

public interface ISocialQueryService
{
    Task<Result<IReadOnlyList<UserSearchResultDto>>> SearchUsersAsync(string keyword, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FriendDto>>> ListFriendsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FriendRequestDto>>> ListIncomingRequestsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FriendRequestDto>>> ListOutgoingRequestsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Result<UserProfilePageDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}
