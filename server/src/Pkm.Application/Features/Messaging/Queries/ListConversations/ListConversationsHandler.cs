using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Queries;

public sealed class ListConversationsHandler
    : IQueryHandler<ListConversationsQuery, ConversationPagedResultDto>
{
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(1);

    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public ListConversationsHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory)
    {
        _currentUser = currentUser;
        _messagingReadRepository = messagingReadRepository;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public async Task<Result<ConversationPagedResultDto>> HandleAsync(
        ListConversationsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<ConversationPagedResultDto>(MessagingErrors.MissingUserContext);

        var page = PageRequest.Normalize(query.PageNumber, query.PageSize, defaultPageSize: 30);
        var version = await _cache.GetAsync<string>(
            MessagingCacheKeys.ConversationListVersion(_cacheKeyFactory, currentUserId),
            cancellationToken) ?? "1";
        var cacheKey = MessagingCacheKeys.ConversationList(_cacheKeyFactory, currentUserId, page.PageNumber, page.PageSize, version);
        var cached = await _cache.GetAsync<ConversationPagedResultDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var items = await _messagingReadRepository.ListConversationsAsync(
            currentUserId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);
        var total = await _messagingReadRepository.CountConversationsAsync(currentUserId, cancellationToken);
        var dto = new ConversationPagedResultDto(
            items,
            page.PageNumber,
            page.PageSize,
            total,
            PageRequest.CalculateTotalPages(total, page.PageSize));

        await _cache.SetAsync(cacheKey, dto, ListCacheTtl, cancellationToken);
        return Result.Success(dto);
    }

}
