using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed class ListIncomingFriendRequestsHandler
    : IQueryHandler<ListIncomingFriendRequestsQuery, FriendRequestPagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IFriendshipRepository _friendshipRepository;

    public ListIncomingFriendRequestsHandler(
        ICurrentUser currentUser,
        IFriendshipRepository friendshipRepository)
    {
        _currentUser = currentUser;
        _friendshipRepository = friendshipRepository;
    }

    public async Task<Result<FriendRequestPagedResultDto>> HandleAsync(
        ListIncomingFriendRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestPagedResultDto>(SocialErrors.MissingUserContext);

        var page = PageRequest.Normalize(query.PageNumber, query.PageSize);

        var items = await _friendshipRepository.ListIncomingRequestsAsync(
            currentUserId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _friendshipRepository.CountIncomingRequestsAsync(
            currentUserId,
            cancellationToken);

        var dto = new FriendRequestPagedResultDto(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize));

        return Result.Success(dto);
    }
}
