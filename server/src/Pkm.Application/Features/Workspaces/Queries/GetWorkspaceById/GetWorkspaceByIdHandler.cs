using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Queries.GetWorkspaceById;

public sealed class GetWorkspaceByIdHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public GetWorkspaceByIdHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        GetWorkspaceByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanRead)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceForbidden);
        }

        var cacheKey = WorkspaceCacheKeys.Detail(_redisKeyFactory, request.WorkspaceId);
        var cached = await _redisCache.GetAsync<WorkspaceDetailReadModel>(cacheKey, cancellationToken);

        WorkspaceDetailReadModel? detail = cached;
        if (detail is null)
        {
            detail = await _workspaceRepository.GetDetailAsync(request.WorkspaceId, cancellationToken);
            if (detail is null)
            {
                return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
            }

            await _redisCache.SetAsync(
                cacheKey,
                detail,
                TimeSpan.FromMinutes(5),
                cancellationToken);
        }

        var currentRole = access.Role;

        var dto = new WorkspaceDto(
            detail.Id,
            detail.Name,
            detail.Description,
            detail.OwnerId,
            detail.LastModifiedBy,
            detail.CreatedDate,
            detail.UpdatedDate,
            currentRole,
            access.CanRead,
            access.CanWrite,
            access.CanManageMembers);

        return Result.Success(dto);
    }
}