using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Pages.Queries.ListArchivedPages;

public sealed class ListArchivedPagesHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageRepository _pageRepository;

    public ListArchivedPagesHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageRepository pageRepository)
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

        var page = NormalizePage(request.PageNumber);
        var size = NormalizeSize(request.PageSize);
        var items = await _pageRepository.ListArchivedByWorkspaceAsync(request.WorkspaceId, page, size, cancellationToken);
        var total = await _pageRepository.CountArchivedByWorkspaceAsync(request.WorkspaceId, cancellationToken);

        return Result.Success(new PagePagedResultDto(items.Select(x => x.ToDto()).ToArray(), page, size, total, CalculateTotalPages(total, size)));
    }

    private static int NormalizePage(int pageNumber) => pageNumber <= 0 ? 1 : pageNumber;
    private static int NormalizeSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
    private static int CalculateTotalPages(int total, int size) => total <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}
