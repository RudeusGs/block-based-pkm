using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed class ListOutgoingFriendRequestsHandler
    : IQueryHandler<ListOutgoingFriendRequestsQuery, IReadOnlyList<FriendRequestDto>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IFriendshipRepository _friendshipRepository;

    public ListOutgoingFriendRequestsHandler(
        ICurrentUser currentUser,
        IFriendshipRepository friendshipRepository)
    {
        _currentUser = currentUser;
        _friendshipRepository = friendshipRepository;
    }

    public async Task<Result<IReadOnlyList<FriendRequestDto>>> HandleAsync(
        ListOutgoingFriendRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<IReadOnlyList<FriendRequestDto>>(SocialErrors.MissingUserContext);

        var page = PageRequest.Normalize(query.PageNumber, query.PageSize);

        var result = await _friendshipRepository.ListOutgoingRequestsAsync(
            currentUserId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
