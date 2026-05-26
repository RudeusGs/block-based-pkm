using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;

public sealed class ListWorkspaceMembersHandler : IQueryHandler<ListWorkspaceMembersQuery, WorkspaceMemberPagedResultDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;

    public ListWorkspaceMembersHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkspaceMemberRepository workspaceMemberRepository)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workspaceMemberRepository = workspaceMemberRepository;
    }

    public async Task<Result<WorkspaceMemberPagedResultDto>> HandleAsync(
        ListWorkspaceMembersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceMemberPagedResultDto>(
                WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceMemberPagedResultDto>(
                WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceMemberPagedResultDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanRead)
        {
            return Result.Failure<WorkspaceMemberPagedResultDto>(
                WorkspaceErrors.WorkspaceForbidden);
        }

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize);

        var members = await _workspaceMemberRepository.ListByWorkspacePagedAsync(
            request.WorkspaceId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _workspaceMemberRepository.CountByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        var dto = new WorkspaceMemberPagedResultDto(
            members.Select(member => member.ToDto(currentUserId)).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize));

        return Result.Success(dto);
    }
}
