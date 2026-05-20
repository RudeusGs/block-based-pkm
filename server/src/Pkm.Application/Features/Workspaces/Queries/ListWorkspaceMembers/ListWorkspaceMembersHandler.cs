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
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public ListWorkspaceMembersHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workspaceMemberRepository = workspaceMemberRepository;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
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

        var cacheKey = WorkspaceCacheKeys.Members(
            _redisKeyFactory,
            request.WorkspaceId);

        var cached = await GetCacheBestEffortAsync<IReadOnlyList<WorkspaceMemberReadModel>>(
            cacheKey,
            cancellationToken);

        IReadOnlyList<WorkspaceMemberReadModel> members;

        if (cached is not null)
        {
            members = cached;
        }
        else
        {
            members = await _workspaceMemberRepository.ListByWorkspaceAsync(
                request.WorkspaceId,
                cancellationToken);

            await SetCacheBestEffortAsync(
                cacheKey,
                members,
                TimeSpan.FromMinutes(5),
                cancellationToken);
        }

        var dto = members
            .Select(member => member.ToDto(currentUserId))
            .ToArray();

        return Result.Success<IReadOnlyList<WorkspaceMemberDto>>(dto);
    }

    private async Task<T?> GetCacheBestEffortAsync<T>(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _redisCache.GetAsync<T>(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return default;
        }
    }

    private async Task SetCacheBestEffortAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        try
        {
            await _redisCache.SetAsync(key, value, ttl, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
        }
    }
}