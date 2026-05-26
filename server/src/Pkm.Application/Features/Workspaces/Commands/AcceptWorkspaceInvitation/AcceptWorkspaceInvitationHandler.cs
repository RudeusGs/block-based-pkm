using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Domain.Audit;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Services;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AcceptWorkspaceInvitation;

public sealed class AcceptWorkspaceInvitationHandler : ICommandHandler<AcceptWorkspaceInvitationCommand, WorkspaceMemberDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceInvitationRepository _workspaceInvitationRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IActivityLogService _activityLogService;
    private readonly INotificationService _notificationService;

    public AcceptWorkspaceInvitationHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IWorkspaceInvitationRepository workspaceInvitationRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        INotificationService notificationService,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _workspaceInvitationRepository = workspaceInvitationRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _activityLogService = activityLogService;
        _notificationService = notificationService;
    }

    public async Task<Result<WorkspaceMemberDto>> HandleAsync(
        AcceptWorkspaceInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvalidInvitationToken);
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.MissingUserContext);
        }

        var tokenHash = WorkspaceInvitationToken.Hash(request.Token);

        var invitation = await _workspaceInvitationRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (invitation is null)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvitationNotFound);
        }

        var now = _clock.UtcNow;

        if (invitation.IsAccepted)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvitationAlreadyAccepted);
        }

        if (invitation.IsExpired(now))
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvitationExpired);
        }

        var targetUser = await _userRepository.GetByIdAsync(
            currentUserId,
            cancellationToken);

        if (targetUser is null)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.TargetUserNotFound);
        }

        if (targetUser.NormalizedEmail != invitation.NormalizedEmail)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.InvitationEmailMismatch);
        }

        var existingMember = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            invitation.WorkspaceId,
            targetUser.Id,
            cancellationToken);

        WorkspaceMember member;

        if (existingMember is not null)
        {
            member = existingMember;
        }
        else
        {
            member = invitation.Role switch
            {
                WorkspaceRole.Manager => WorkspaceMember.CreateManager(invitation.WorkspaceId, targetUser.Id, now),
                WorkspaceRole.Member => WorkspaceMember.CreateMember(invitation.WorkspaceId, targetUser.Id, now),
                WorkspaceRole.Viewer => WorkspaceMember.CreateViewer(invitation.WorkspaceId, targetUser.Id, now),
                _ => WorkspaceMember.CreateMember(invitation.WorkspaceId, targetUser.Id, now)
            };

            _workspaceMemberRepository.Add(member);
        }

        invitation.Accept(targetUser.Id, now);
        _workspaceInvitationRepository.Update(invitation);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                invitation.WorkspaceId,
                targetUser.Id,
                ActivityAction.Create,
                ActivityEntityType.WorkspaceMember,
                targetUser.Id,
                $"{targetUser.FullName} đã tham gia workspace."),
            cancellationToken);

        await InvalidateWorkspaceMemberCachesAsync(
            invitation.WorkspaceId,
            targetUser.Id,
            cancellationToken);

        return Result.Success(member.ToDto(targetUser, targetUser.Id));
    }

    private async Task InvalidateWorkspaceMemberCachesAsync(
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
