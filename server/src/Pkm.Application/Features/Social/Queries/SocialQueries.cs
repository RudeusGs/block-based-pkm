using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;
using Pkm.Application.Features.Social.Services;

namespace Pkm.Application.Features.Social.Queries;

public sealed record SearchUsersQuery(
    string Keyword,
    int PageNumber,
    int PageSize) : IQuery<IReadOnlyList<UserSearchResultDto>>;

public sealed class SearchUsersHandler
    : IQueryHandler<SearchUsersQuery, IReadOnlyList<UserSearchResultDto>>
{
    private readonly ISocialQueryService _socialQueryService;

    public SearchUsersHandler(ISocialQueryService socialQueryService)
    {
        _socialQueryService = socialQueryService;
    }

    public Task<Result<IReadOnlyList<UserSearchResultDto>>> HandleAsync(
        SearchUsersQuery query,
        CancellationToken cancellationToken = default)
        => _socialQueryService.SearchUsersAsync(
            query.Keyword,
            query.PageNumber,
            query.PageSize,
            cancellationToken);
}

public sealed record GetProfileQuery(Guid UserId)
    : IQuery<UserProfilePageDto>;

public sealed class GetProfileHandler
    : IQueryHandler<GetProfileQuery, UserProfilePageDto>
{
    private readonly ISocialQueryService _socialQueryService;

    public GetProfileHandler(ISocialQueryService socialQueryService)
    {
        _socialQueryService = socialQueryService;
    }

    public Task<Result<UserProfilePageDto>> HandleAsync(
        GetProfileQuery query,
        CancellationToken cancellationToken = default)
        => _socialQueryService.GetProfileAsync(
            query.UserId,
            cancellationToken);
}

public sealed record ListIncomingFriendRequestsQuery(int PageNumber, int PageSize)
    : IQuery<IReadOnlyList<FriendRequestDto>>;

public sealed class ListIncomingFriendRequestsHandler
    : IQueryHandler<ListIncomingFriendRequestsQuery, IReadOnlyList<FriendRequestDto>>
{
    private readonly ISocialQueryService _socialQueryService;

    public ListIncomingFriendRequestsHandler(ISocialQueryService socialQueryService)
    {
        _socialQueryService = socialQueryService;
    }

    public Task<Result<IReadOnlyList<FriendRequestDto>>> HandleAsync(
        ListIncomingFriendRequestsQuery query,
        CancellationToken cancellationToken = default)
        => _socialQueryService.ListIncomingRequestsAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);
}

public sealed record ListOutgoingFriendRequestsQuery(int PageNumber, int PageSize)
    : IQuery<IReadOnlyList<FriendRequestDto>>;

public sealed class ListOutgoingFriendRequestsHandler
    : IQueryHandler<ListOutgoingFriendRequestsQuery, IReadOnlyList<FriendRequestDto>>
{
    private readonly ISocialQueryService _socialQueryService;

    public ListOutgoingFriendRequestsHandler(ISocialQueryService socialQueryService)
    {
        _socialQueryService = socialQueryService;
    }

    public Task<Result<IReadOnlyList<FriendRequestDto>>> HandleAsync(
        ListOutgoingFriendRequestsQuery query,
        CancellationToken cancellationToken = default)
        => _socialQueryService.ListOutgoingRequestsAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);
}

public sealed record ListFriendsQuery(int PageNumber, int PageSize)
    : IQuery<IReadOnlyList<FriendDto>>;

public sealed class ListFriendsHandler
    : IQueryHandler<ListFriendsQuery, IReadOnlyList<FriendDto>>
{
    private readonly ISocialQueryService _socialQueryService;

    public ListFriendsHandler(ISocialQueryService socialQueryService)
    {
        _socialQueryService = socialQueryService;
    }

    public Task<Result<IReadOnlyList<FriendDto>>> HandleAsync(
        ListFriendsQuery query,
        CancellationToken cancellationToken = default)
        => _socialQueryService.ListFriendsAsync(
            query.PageNumber,
            query.PageSize,
            cancellationToken);
}
