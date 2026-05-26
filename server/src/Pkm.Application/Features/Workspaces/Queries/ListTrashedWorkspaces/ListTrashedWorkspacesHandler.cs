using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;

namespace Pkm.Application.Features.Workspaces.Queries.ListTrashedWorkspaces;

public sealed class ListTrashedWorkspacesHandler : IQueryHandler<ListTrashedWorkspacesQuery, WorkspaceTrashPagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;

    public ListTrashedWorkspacesHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<Result<WorkspaceTrashPagedResultDto>> HandleAsync(
        ListTrashedWorkspacesQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceTrashPagedResultDto>(WorkspaceErrors.MissingUserContext);
        }

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize);

        var items = await _workspaceRepository.ListTrashedByOwnerAsync(
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

        return Result.Success(new WorkspaceTrashPagedResultDto(
            page.PageNumber,
            page.PageSize,
            totalCount,
            MapItems(items)));
    }

    private async Task<int> ResolveTotalCountAsync(
        Guid currentUserId,
        int pageNumber,
        int pageSize,
        IReadOnlyList<WorkspaceTrashItemReadModel> items,
        CancellationToken cancellationToken)
    {
        if (pageNumber == PageRequest.DefaultPageNumber && items.Count < pageSize)
        {
            return items.Count;
        }

        return await _workspaceRepository.CountTrashedByOwnerAsync(currentUserId, cancellationToken);
    }

    private static IReadOnlyList<WorkspaceTrashItemDto> MapItems(
        IReadOnlyList<WorkspaceTrashItemReadModel> items)
    {
        if (items.Count == 0)
        {
            return Array.Empty<WorkspaceTrashItemDto>();
        }

        var result = new WorkspaceTrashItemDto[items.Count];

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            result[i] = new WorkspaceTrashItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.AvatarUrl,
                item.Visibility,
                item.OwnerId,
                item.CreatedDate,
                item.UpdatedDate,
                item.TrashedAt,
                item.CurrentUserRole);
        }

        return result;
    }
}
