using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Workspaces.Commands.RemoveWorkspaceMember;

public sealed class RemoveWorkspaceMemberHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly INotificationService _notificationService;
    public RemoveWorkspaceMemberHandler(
        ICurrentUser currentUser,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _notificationService = notificationService;
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

        member.SoftDelete(_clock.UtcNow);
        _workspaceMemberRepository.Update(member);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Members(_redisKeyFactory, request.WorkspaceId),
            cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Access(_redisKeyFactory, request.WorkspaceId, request.UserId),
            cancellationToken);

        await _notificationService.NotifyAsync(
            request.UserId,
            NotificationTemplates.WorkspaceMemberRemoved(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                request.WorkspaceId),
            cancellationToken);

        var targetVersionKey = WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, request.UserId);
        await _redisCache.SetAsync(
            targetVersionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
        
        return Result.Success();
    }
}