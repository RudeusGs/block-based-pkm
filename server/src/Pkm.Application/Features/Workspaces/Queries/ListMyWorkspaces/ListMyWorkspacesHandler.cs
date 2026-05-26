using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.ListMyWorkspaces;

public sealed class ListMyWorkspacesHandler : IQueryHandler<ListMyWorkspacesQuery, WorkspacePagedResultDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public ListMyWorkspacesHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public async Task<Result<WorkspacePagedResultDto>> HandleAsync(
        ListMyWorkspacesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspacePagedResultDto>(WorkspaceErrors.MissingUserContext);
        }

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize);

        var versionKey = WorkspaceCacheKeys.UserListVersion(_cacheKeyFactory, currentUserId);
        var version = await _cache.GetAsync<string>(versionKey, cancellationToken) ?? "1";

        var cacheKey = WorkspaceCacheKeys.List(
            _cacheKeyFactory,
            currentUserId,
            page.PageNumber,
            page.PageSize,
            version);

        var cached = await _cache.GetAsync<WorkspacePagedResultDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result.Success(cached);
        }

        var items = await _workspaceRepository.ListByUserAsync(
            currentUserId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await ResolveTotalCountAsync(
            currentUserId,
            page.PageNumber,
            page.PageSize,
            items,
            cancellationToken);

        var dto = new WorkspacePagedResultDto(
            page.PageNumber,
            page.PageSize,
            totalCount,
            MapItems(items));

        await _cache.SetAsync(
            cacheKey,
            dto,
            CacheTtl,
            cancellationToken);

        return Result.Success(dto);
    }

    private async Task<int> ResolveTotalCountAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        IReadOnlyList<WorkspaceListItemReadModel> items,
        CancellationToken cancellationToken)
    {
        if (pageNumber == PageRequest.DefaultPageNumber && items.Count < pageSize)
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
                item.Visibility,
                item.OwnerId,
                item.CreatedDate,
                item.UpdatedDate,
                item.CurrentUserRole);
        }

        return result;
    }
}
