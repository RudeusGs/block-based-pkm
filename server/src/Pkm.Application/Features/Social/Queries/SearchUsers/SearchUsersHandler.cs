using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed class SearchUsersHandler
    : IQueryHandler<SearchUsersQuery, UserSearchResultPagedResultDto>
{
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromSeconds(20);

    private readonly ICurrentUser _currentUser;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public SearchUsersHandler(
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

    public async Task<Result<UserSearchResultPagedResultDto>> HandleAsync(
        SearchUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserSearchResultPagedResultDto>(SocialErrors.MissingUserContext);

        var normalizedKeyword = (query.Keyword ?? string.Empty).Trim();
        if (normalizedKeyword.Length < 2)
            return Result.Failure<UserSearchResultPagedResultDto>(
                SocialErrors.InvalidRequest(new[] { "Từ khóa tìm kiếm phải có ít nhất 2 ký tự." }));

        var page = PageRequest.Normalize(query.PageNumber, query.PageSize);
        var cacheKey = SocialCacheKeys.UserSearch(
            _cacheKeyFactory,
            currentUserId,
            normalizedKeyword,
            page.PageNumber,
            page.PageSize);

        var cached = await _cache.GetAsync<UserSearchResultPagedResultDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var items = await _friendshipRepository.SearchUsersAsync(
            currentUserId,
            normalizedKeyword,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _friendshipRepository.CountSearchUsersAsync(
            currentUserId,
            normalizedKeyword,
            cancellationToken);

        var dto = new UserSearchResultPagedResultDto(
            items,
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize));

        await _cache.SetAsync(cacheKey, dto, SearchCacheTtl, cancellationToken);
        return Result.Success(dto);
    }
}
