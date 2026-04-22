using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Pages.Queries.SearchPages;

public sealed class SearchPagesHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageRepository _pageRepository;

    public SearchPagesHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageRepository pageRepository)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageRepository = pageRepository;
    }

    public async Task<Result<PagePagedResultDto>> HandleAsync(
        SearchPagesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
            return Result.Failure<PagePagedResultDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));

        if (string.IsNullOrWhiteSpace(request.Keyword))
            return Result.Failure<PagePagedResultDto>(PageErrors.InvalidSearchKeyword);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PagePagedResultDto>(PageErrors.MissingUserContext);

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PagePagedResultDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanRead)
            return Result.Failure<PagePagedResultDto>(WorkspaceErrors.WorkspaceForbidden);

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);

        var items = await _pageRepository.SearchInWorkspaceAsync(
            request.WorkspaceId,
            request.Keyword,
            pageNumber,
            pageSize,
            cancellationToken);

        var totalCount = await _pageRepository.CountSearchInWorkspaceAsync(
            request.WorkspaceId,
            request.Keyword,
            cancellationToken);

        var dto = new PagePagedResultDto(
            items.Select(x => x.ToDto()).ToArray(),
            pageNumber,
            pageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));

        return Result.Success(dto);
    }
}