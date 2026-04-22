using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Authorization;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Policies;

public sealed class WorkspaceAccessEvaluator : IWorkspaceAccessEvaluator
{
    private static readonly TimeSpan AccessCacheTtl = TimeSpan.FromSeconds(30);

    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public WorkspaceAccessEvaluator(
        IWorkspaceRepository workspaceRepository,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _workspaceRepository = workspaceRepository;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
    }

    public async Task<WorkspaceAccessResult> EvaluateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty || userId == Guid.Empty)
        {
            return new WorkspaceAccessResult(
                Exists: false,
                WorkspaceId: Guid.Empty,
                Role: null,
                CanReadWorkspace: false,
                CanUpdateWorkspace: false,
                CanDeleteWorkspace: false,
                CanManageMembers: false,
                CanManageWorkspaceSettings: false,
                CanCreatePages: false,
                CanCreateTasks: false,
                CanReadAudit: false,
                CanManageAuditRetention: false);
        }

        var cacheKey = WorkspaceCacheKeys.Access(_redisKeyFactory, workspaceId, userId);
        var cached = await _redisCache.GetAsync<WorkspaceAccessResult>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var access = await _workspaceRepository.GetAccessContextAsync(
            workspaceId,
            userId,
            cancellationToken);

        WorkspaceAccessResult result;

        if (access is null)
        {
            result = new WorkspaceAccessResult(
                Exists: false,
                WorkspaceId: Guid.Empty,
                Role: null,
                CanReadWorkspace: false,
                CanUpdateWorkspace: false,
                CanDeleteWorkspace: false,
                CanManageMembers: false,
                CanManageWorkspaceSettings: false,
                CanCreatePages: false,
                CanCreateTasks: false,
                CanReadAudit: false,
                CanManageAuditRetention: false);
        }
        else
        {
            var isOwner = access.OwnerId == userId || access.Role == WorkspaceRole.Owner;
            var capabilities = WorkspaceRoleCapabilityMatrix.ForWorkspace(isOwner, access.Role);

            result = new WorkspaceAccessResult(
                Exists: true,
                WorkspaceId: access.WorkspaceId,
                Role: isOwner ? WorkspaceRole.Owner : access.Role,
                CanReadWorkspace: capabilities.CanReadWorkspace,
                CanUpdateWorkspace: capabilities.CanUpdateWorkspace,
                CanDeleteWorkspace: capabilities.CanDeleteWorkspace,
                CanManageMembers: capabilities.CanManageMembers,
                CanManageWorkspaceSettings: capabilities.CanManageWorkspaceSettings,
                CanCreatePages: capabilities.CanCreatePages,
                CanCreateTasks: capabilities.CanCreateTasks,
                CanReadAudit: capabilities.CanReadAudit,
                CanManageAuditRetention: capabilities.CanManageAuditRetention);
        }

        await _redisCache.SetAsync(cacheKey, result, AccessCacheTtl, cancellationToken);
        return result;
    }
}