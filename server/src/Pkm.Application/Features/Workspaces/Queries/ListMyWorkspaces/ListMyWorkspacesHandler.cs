using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;

public sealed class ListMyWorkspacesHandler
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const int DefaultPageNumber = 1;
    private const int DefaultPageSize = 20;

    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public ListMyWorkspacesHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
    }

    public async Task<Result<WorkspacePagedResultDto>> HandleAsync(
        ListMyWorkspacesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspacePagedResultDto>(WorkspaceErrors.MissingUserContext);
        }

        var pageNumber = NormalizePageNumber(request.PageNumber);
        var pageSize = NormalizePageSize(request.PageSize);

        var versionKey = WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, currentUserId);
        var version = await _redisCache.GetAsync<string>(versionKey, cancellationToken) ?? "1";

        var cacheKey = WorkspaceCacheKeys.List(
            _redisKeyFactory,
            currentUserId,
            pageNumber,
            pageSize,
            version);

        var cached = await _redisCache.GetAsync<WorkspacePagedResultDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var items = await _workspaceRepository.ListByUserAsync(
            currentUserId,
            pageNumber,
            pageSize,
            cancellationToken);

        var totalCount = await ResolveTotalCountAsync(
            currentUserId,
            pageNumber,
            pageSize,
            items,
            cancellationToken);

        var dto = new WorkspacePagedResultDto(
            pageNumber,
            pageSize,
            totalCount,
            MapItems(items));

        await _redisCache.SetAsync(
            cacheKey,
            dto,
            CacheTtl,
            cancellationToken);

        return Result.Success(dto);
    }

    private static int NormalizePageNumber(int pageNumber)
        => pageNumber > 0 ? pageNumber : DefaultPageNumber;

    private static int NormalizePageSize(int pageSize)
        => pageSize > 0 ? pageSize : DefaultPageSize;

    private async Task<int> ResolveTotalCountAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        IReadOnlyList<WorkspaceListItemReadModel> items,
        CancellationToken cancellationToken)
    {
        if (pageNumber == DefaultPageNumber && items.Count < pageSize)
        {
            return items.Count;
        }

        return await _workspaceRepository.CountByUserAsync(currentUserId, cancellationToken);
    }

    private static IReadOnlyList<WorkspaceListItemDto> MapItems(
        IReadOnlyList<WorkspaceListItemReadModel> items)
    {
        if (items.Count == 0)
        {
            return Array.Empty<WorkspaceListItemDto>();
        }

        var result = new WorkspaceListItemDto[items.Count];

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            result[i] = new WorkspaceListItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.OwnerId,
                item.CreatedDate,
                item.UpdatedDate,
                item.CurrentUserRole);
        }

        return result;
    }
}