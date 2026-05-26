using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;

public sealed class RemoveWorkspaceMemberHandler : ICommandHandler<RemoveWorkspaceMemberCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogService _activityLogService;
    public RemoveWorkspaceMemberHandler(
        ICurrentUser currentUser,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        INotificationService notificationService,
        IUserRepository userRepository,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _activityLogService = activityLogService;
    }

    public async Task<Result> HandleAsync(
        RemoveWorkspaceMemberCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (request.UserId == Guid.Empty)
        {
            return Result.Failure(WorkspaceErrors.InvalidUserId(request.UserId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure(WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanManageMembers)
        {
            return Result.Failure(WorkspaceErrors.WorkspaceManageMembersForbidden);
        }

        if (request.UserId == currentUserId)
        {
            return Result.Failure(WorkspaceErrors.CannotManageYourself);
        }

        var member = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            request.WorkspaceId,
            request.UserId,
            cancellationToken);

        if (member is null)
        {
            return Result.Failure(WorkspaceErrors.WorkspaceMemberNotFound);
        }

        if (member.IsOwner())
        {
            return Result.Failure(WorkspaceErrors.CannotModifyOwnerMembership);
        }

        var removedRole = member.Role;
        var targetUser = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        member.SoftDelete(_clock.UtcNow);
        _workspaceMemberRepository.Update(member);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync(
            WorkspaceCacheKeys.Members(_cacheKeyFactory, request.WorkspaceId),
            cancellationToken);

        await _cache.RemoveAsync(
            WorkspaceCacheKeys.Access(_cacheKeyFactory, request.WorkspaceId, request.UserId),
            cancellationToken);

        await _notificationService.NotifyAsync(
            request.UserId,
            NotificationTemplates.WorkspaceMemberRemoved(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                request.WorkspaceId),
            cancellationToken);

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                request.WorkspaceId,
                currentUserId,
                ActivityAction.Delete,
                ActivityEntityType.WorkspaceMember,
                request.UserId,
                $"{_currentUser.UserName ?? "Có người"} đã xóa {targetUser?.FullName ?? "một thành viên"} khỏi workspace.",
                ActivityLogMetadata.Serialize(new
                {
                    targetUserId = request.UserId,
                    targetEmail = targetUser?.Email,
                    role = removedRole.ToString()
                })),
            cancellationToken);

        var targetVersionKey = WorkspaceCacheKeys.UserListVersion(_cacheKeyFactory, request.UserId);
        await _cache.SetAsync(
            targetVersionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);

        return Result.Success();
    }
}
