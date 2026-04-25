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
namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed class AddWorkspaceMemberHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly AddWorkspaceMemberCommandValidator _validator;
    private readonly INotificationService _notificationService;
    public AddWorkspaceMemberHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        AddWorkspaceMemberCommandValidator validator,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _validator = validator;
        _notificationService = notificationService;
    }

    public async Task<Result<WorkspaceMemberDto>> HandleAsync(
        AddWorkspaceMemberCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvalidAddMemberRequest(validationErrors));
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

        var targetUser = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (targetUser is null)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.TargetUserNotFound);
        }

        var exists = await _workspaceMemberRepository.ExistsAsync(
            request.WorkspaceId,
            request.UserId,
            cancellationToken);

        if (exists)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.WorkspaceMemberAlreadyExists);
        }

        var now = _clock.UtcNow;
        var member = request.Role switch
        {
            WorkspaceRole.Manager => WorkspaceMember.CreateManager(request.WorkspaceId, request.UserId, now),
            WorkspaceRole.Member => WorkspaceMember.CreateMember(request.WorkspaceId, request.UserId, now),
            WorkspaceRole.Viewer => WorkspaceMember.CreateViewer(request.WorkspaceId, request.UserId, now),
            _ => WorkspaceMember.CreateMember(request.WorkspaceId, request.UserId, now)
        };

        _workspaceMemberRepository.Add(member);
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