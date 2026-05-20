using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Services;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AcceptWorkspaceInvitation;

public sealed class AcceptWorkspaceInvitationHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceInvitationRepository _workspaceInvitationRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly INotificationService _notificationService;

    public AcceptWorkspaceInvitationHandler(
        IUserRepository userRepository,
        IWorkspaceInvitationRepository workspaceInvitationRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        INotificationService notificationService)
    {
        _userRepository = userRepository;
        _workspaceInvitationRepository = workspaceInvitationRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
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

        var targetUser = await _userRepository.GetByEmailAsync(
            invitation.Email,
            cancellationToken);

        if (targetUser is null)
        {
            return Result.Failure<WorkspaceMemberDto>(WorkspaceErrors.TargetUserNotFoundByEmail(invitation.Email));
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

        await InvalidateWorkspaceMemberCachesAsync(
            invitation.WorkspaceId,
            targetUser.Id,
            cancellationToken);

        return Result.Success(ToDto(member));
    }

    private async Task InvalidateWorkspaceMemberCachesAsync(
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

    private static WorkspaceMemberDto ToDto(WorkspaceMember member)
        => new(
            member.WorkspaceId,
            member.UserId,
            member.Role,
            member.IsOwner(),
            member.CreatedDate,
            member.UpdatedDate);
}
