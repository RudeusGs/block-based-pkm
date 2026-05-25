using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Authorization;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.JoinPublicWorkspaceAsViewer;

public sealed class JoinPublicWorkspaceAsViewerHandler : ICommandHandler<JoinPublicWorkspaceAsViewerCommand, WorkspaceDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public JoinPublicWorkspaceAsViewerHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        JoinPublicWorkspaceAsViewerCommand request,
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

        var workspace = await _workspaceRepository.GetDetailAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        var existingMember = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        var isOwner = workspace.OwnerId == currentUserId || existingMember?.Role == WorkspaceRole.Owner;

        if (existingMember is null && !isOwner)
        {
            if (workspace.Visibility != WorkspaceVisibility.Public)
            {
                return Result.Failure<WorkspaceDto>(WorkspaceErrors.PublicWorkspaceJoinOnly);
            }

            var now = _clock.UtcNow;
            var member = WorkspaceMember.CreateViewer(request.WorkspaceId, currentUserId, now);

            _workspaceMemberRepository.Add(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    request.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.WorkspaceMember,
                    currentUserId,
                    $"{_currentUser.UserName ?? "Có người"} đã tham gia public workspace với vai trò Viewer.",
                    ActivityLogMetadata.Serialize(new
                    {
                        source = "PublicProfileWorkspace",
                        role = WorkspaceRole.Viewer.ToString()
                    })),
                cancellationToken);
        }

        var effectiveRole = isOwner
            ? WorkspaceRole.Owner
            : existingMember?.Role ?? WorkspaceRole.Viewer;

        await InvalidateWorkspaceMembershipCachesAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        var capabilities = WorkspaceRoleCapabilityMatrix.ForWorkspace(isOwner, effectiveRole);

        var dto = new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.Visibility,
            workspace.OwnerId,
            workspace.LastModifiedBy,
            workspace.CreatedDate,
            workspace.UpdatedDate,
            effectiveRole,
            capabilities.CanReadWorkspace,
            capabilities.CanUpdateWorkspace,
            capabilities.CanManageMembers);

        return Result.Success(dto);
    }

    private async Task InvalidateWorkspaceMembershipCachesAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Members(_redisKeyFactory, workspaceId),
            cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Access(_redisKeyFactory, workspaceId, userId),
            cancellationToken);

        await _redisCache.SetAsync(
            WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, userId),
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }
}
