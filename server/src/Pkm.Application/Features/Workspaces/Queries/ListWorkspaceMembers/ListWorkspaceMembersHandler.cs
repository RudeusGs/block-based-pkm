using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Workspaces.Queries.ListWorkspaceMembers;

public sealed class ListWorkspaceMembersHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public ListWorkspaceMembersHandler(
        ICurrentUser currentUser,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
    }

    public async Task<Result<IReadOnlyList<WorkspaceMemberDto>>> HandleAsync(
        ListWorkspaceMembersQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanRead)
        {
            return Result.Failure<IReadOnlyList<WorkspaceMemberDto>>(WorkspaceErrors.WorkspaceForbidden);
        }

        var cacheKey = WorkspaceCacheKeys.Members(_redisKeyFactory, request.WorkspaceId);
        var cached = await _redisCache.GetAsync<IReadOnlyList<WorkspaceMemberReadModel>>(cacheKey, cancellationToken);

        IReadOnlyList<WorkspaceMemberReadModel> members = cached ?? Array.Empty<WorkspaceMemberReadModel>();

        if (cached is null)
        {
            members = await _workspaceMemberRepository.ListReadModelsByWorkspaceAsync(
                request.WorkspaceId,
                cancellationToken);

            await _redisCache.SetAsync(
                cacheKey,
                members,
                TimeSpan.FromMinutes(2),
                cancellationToken);
        }

        var dto = members
            .OrderBy(x => x.Role)
            .ThenBy(x => x.CreatedDate)
            .Select(x => new WorkspaceMemberDto(
                x.WorkspaceId,
                x.UserId,
                x.Role,
                x.Role == Pkm.Domain.Workspaces.WorkspaceRole.Owner,
                x.CreatedDate,
                x.UpdatedDate))
            .ToArray();

        return Result.Success<IReadOnlyList<WorkspaceMemberDto>>(dto);
    }
}