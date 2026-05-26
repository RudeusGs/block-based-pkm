using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Pages.Queries.ListArchivedPages;

public sealed class ListArchivedPagesHandler : IQueryHandler<ListArchivedPagesQuery, PagePagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageReadRepository _pageRepository;

    public ListArchivedPagesHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageReadRepository pageRepository)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageRepository = pageRepository;
    }

    public async Task<Result<PagePagedResultDto>> HandleAsync(ListArchivedPagesQuery request, CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
            return Result.Failure<PagePagedResultDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PagePagedResultDto>(PageErrors.MissingUserContext);

        var access = await _workspaceAccessEvaluator.EvaluateAsync(request.WorkspaceId, currentUserId, cancellationToken);
        if (!access.Exists)
            return Result.Failure<PagePagedResultDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanReadWorkspace)
            return Result.Failure<PagePagedResultDto>(WorkspaceErrors.WorkspaceForbidden);

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize);
        var items = await _pageRepository.ListArchivedByWorkspaceAsync(request.WorkspaceId, page.PageNumber, page.PageSize, cancellationToken);
        var total = await _pageRepository.CountArchivedByWorkspaceAsync(request.WorkspaceId, cancellationToken);

        return Result.Success(new PagePagedResultDto(
            items.Select(x => x.ToDto()).ToArray(),
            page.PageNumber,
            page.PageSize,
            total,
            PageRequest.CalculateTotalPages(total, page.PageSize)));
    }
}
