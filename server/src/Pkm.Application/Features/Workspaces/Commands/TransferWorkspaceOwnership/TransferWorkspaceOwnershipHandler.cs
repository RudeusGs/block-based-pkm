using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Authorization;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.TransferWorkspaceOwnership;

public sealed class TransferWorkspaceOwnershipHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public TransferWorkspaceOwnershipHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        TransferWorkspaceOwnershipCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (request.NewOwnerUserId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidUserId(request.NewOwnerUserId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.MissingUserContext);
        }

        var workspace = await _workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (workspace.OwnerId != currentUserId)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceOwnerOnly);
        }

        if (request.NewOwnerUserId == currentUserId)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.CannotTransferOwnershipToYourself);
        }

        var newOwnerUser = await _userRepository.GetByIdAsync(request.NewOwnerUserId, cancellationToken);
        if (newOwnerUser is null)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.TargetUserNotFound);
        }

        var oldOwnerMember = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        var newOwnerMember = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            request.WorkspaceId,
            request.NewOwnerUserId,
            cancellationToken);

        if (newOwnerMember is null)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.NewOwnerMustBeWorkspaceMember);
        }

        var now = _clock.UtcNow;
        workspace.TransferOwnership(request.NewOwnerUserId, currentUserId, now);
        _workspaceRepository.Update(workspace);

        newOwnerMember.ChangeRole(WorkspaceRole.Owner, now);
        _workspaceMemberRepository.Update(newOwnerMember);

        if (oldOwnerMember is not null)
        {
            oldOwnerMember.ChangeRole(WorkspaceRole.Manager, now);
            _workspaceMemberRepository.Update(oldOwnerMember);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateWorkspaceCachesAsync(request.WorkspaceId, currentUserId, cancellationToken);
        await InvalidateWorkspaceCachesAsync(request.WorkspaceId, request.NewOwnerUserId, cancellationToken);
        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Detail(_redisKeyFactory, request.WorkspaceId),
            cancellationToken);

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                request.WorkspaceId,
                currentUserId,
                ActivityAction.ChangePermissions,
                ActivityEntityType.Workspace,
                request.WorkspaceId,
                $"{_currentUser.UserName ?? "Có người"} đã chuyển quyền owner workspace cho {newOwnerUser.FullName}.",
                ActivityLogMetadata.Serialize(new
                {
                    oldOwnerId = currentUserId,
                    newOwnerId = request.NewOwnerUserId,
                    newOwnerEmail = newOwnerUser.Email
                })),
            cancellationToken);

        var capabilities = WorkspaceRoleCapabilityMatrix.ForWorkspace(isOwner: false, role: WorkspaceRole.Manager);
        var dto = new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.Visibility,
            workspace.OwnerId,
            workspace.LastModifiedBy,
            workspace.CreatedDate,
            workspace.UpdatedDate,
            WorkspaceRole.Manager,
            capabilities.CanReadWorkspace,
            capabilities.CanUpdateWorkspace,
            capabilities.CanManageMembers);

        return Result.Success(dto);
    }

    private async Task InvalidateWorkspaceCachesAsync(
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
