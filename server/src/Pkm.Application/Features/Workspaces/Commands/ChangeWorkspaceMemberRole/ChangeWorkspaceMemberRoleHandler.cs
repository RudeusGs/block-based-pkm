using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Workspaces;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
namespace Pkm.Application.Features.Workspaces.Commands.ChangeWorkspaceMemberRole;

public sealed class ChangeWorkspaceMemberRoleHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly INotificationService _notificationService;
    public ChangeWorkspaceMemberRoleHandler(
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

    public async Task<Result<WorkspaceMemberDto>> HandleAsync(
        ChangeWorkspaceMemberRoleCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = new List<string>();

        if (request.WorkspaceId == Guid.Empty)
            validationErrors.Add("WorkspaceId không hợp lệ.");

        if (request.UserId == Guid.Empty)
            validationErrors.Add("UserId không hợp lệ.");

        if (!Enum.IsDefined(typeof(WorkspaceRole), request.Role))
            validationErrors.Add("Vai trò không hợp lệ.");

        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvalidRoleChangeRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanManageMembers)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.WorkspaceManageMembersForbidden);
        }

        if (request.UserId == currentUserId)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.CannotManageYourself);
        }

        if (request.Role == WorkspaceRole.Owner)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.CannotAssignOwnerRole);
        }

        var member = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            request.WorkspaceId,
            request.UserId,
            cancellationToken);

        if (member is null)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.WorkspaceMemberNotFound);
        }

        if (member.IsOwner())
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.CannotModifyOwnerMembership);
        }

        member.ChangeRole(request.Role, _clock.UtcNow);
        _workspaceMemberRepository.Update(member);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Members(_redisKeyFactory, request.WorkspaceId),
            cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Access(_redisKeyFactory, request.WorkspaceId, request.UserId),
            cancellationToken);

        var targetVersionKey = WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, request.UserId);
        await _redisCache.SetAsync(
            targetVersionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);

        var dto = new WorkspaceMemberDto(
            member.WorkspaceId,
            member.UserId,
            member.Role,
            member.IsOwner(),
            member.CreatedDate,
            member.UpdatedDate);

        await _notificationService.NotifyAsync(
            request.UserId,
            NotificationTemplates.WorkspaceRoleChanged(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                request.WorkspaceId,
                request.Role),
            cancellationToken);

        return Result.Success(dto);
    }
}