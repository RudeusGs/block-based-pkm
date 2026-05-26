using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed class ListFriendsHandler
    : IQueryHandler<ListFriendsQuery, FriendPagedResultDto>
{
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(2);

    private readonly ICurrentUser _currentUser;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public ListFriendsHandler(
        ICurrentUser currentUser,
        IFriendshipRepository friendshipRepository,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory)
    {
        _currentUser = currentUser;
        _friendshipRepository = friendshipRepository;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public async Task<Result<FriendPagedResultDto>> HandleAsync(
        ListFriendsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendPagedResultDto>(SocialErrors.MissingUserContext);

        var page = PageRequest.Normalize(query.PageNumber, query.PageSize);
        var version = await _cache.GetAsync<string>(
            SocialCacheKeys.FriendListVersion(_cacheKeyFactory, currentUserId),
            cancellationToken) ?? "1";

        var cacheKey = SocialCacheKeys.FriendList(_cacheKeyFactory, currentUserId, page.PageNumber, page.PageSize, version);
        var cached = await _cache.GetAsync<FriendPagedResultDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var friends = await _friendshipRepository.ListFriendsAsync(
            currentUserId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _friendshipRepository.CountFriendsAsync(
            currentUserId,
            cancellationToken);

        var dto = new FriendPagedResultDto(
            friends,
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize));

        await _cache.SetAsync(cacheKey, dto, ListCacheTtl, cancellationToken);
        return Result.Success(dto);
    }
}
