using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Workspaces.Commands.LeaveWorkspace;

public sealed class LeaveWorkspaceHandler : ICommandHandler<LeaveWorkspaceCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public LeaveWorkspaceHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result> HandleAsync(
        LeaveWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure(WorkspaceErrors.MissingUserContext);
        }

        var workspace = await _workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(WorkspaceErrors.WorkspaceNotFound);
        }

        if (workspace.OwnerId == currentUserId)
        {
            return Result.Failure(WorkspaceErrors.CannotLeaveOwnedWorkspace);
        }

        var member = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (member is null)
        {
            return Result.Failure(WorkspaceErrors.WorkspaceMemberNotFound);
        }

        var removedRole = member.Role;
        member.SoftDelete(_clock.UtcNow);
        _workspaceMemberRepository.Update(member);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await InvalidateMembershipCachesAsync(request.WorkspaceId, currentUserId, cancellationToken);

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                request.WorkspaceId,
                currentUserId,
                ActivityAction.Unassign,
                ActivityEntityType.WorkspaceMember,
                currentUserId,
                $"{_currentUser.UserName ?? "Có người"} đã rời workspace.",
                ActivityLogMetadata.Serialize(new
                {
                    userId = currentUserId,
                    role = removedRole.ToString(),
                    source = "LeaveWorkspace"
                })),
            cancellationToken);

        return Result.Success();
    }

    private async Task InvalidateMembershipCachesAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            WorkspaceCacheKeys.Members(_cacheKeyFactory, workspaceId),
            cancellationToken);

        await _cache.RemoveAsync(
            WorkspaceCacheKeys.Access(_cacheKeyFactory, workspaceId, userId),
            cancellationToken);

        await _cache.SetAsync(
            WorkspaceCacheKeys.UserListVersion(_cacheKeyFactory, userId),
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }
}
