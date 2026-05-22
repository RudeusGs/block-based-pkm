using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;

public sealed class ListWorkspaceMembersHandler
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

    public async Task<Result<IReadOnlyList<WorkspaceMemberDto>>> HandleAsync(
        ListWorkspaceMembersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(
                WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(
                WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanRead)
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(
                WorkspaceErrors.WorkspaceForbidden);
        }

        var members = await _workspaceMemberRepository.ListByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        var dto = members
            .Select(member => member.ToDto(currentUserId))
            .ToArray();

        return Result.Success<IReadOnlyList<WorkspaceMemberDto>>(dto);
    }
}
