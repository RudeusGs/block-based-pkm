using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Email;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Application.Features.Workspaces.Services;
using Pkm.Domain.Audit;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed class AddWorkspaceMemberHandler : ICommandHandler<AddWorkspaceMemberCommand, WorkspaceInvitationDto>
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceInvitationRepository _workspaceInvitationRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly AddWorkspaceMemberCommandValidator _validator;
    private readonly IEmailSender _emailSender;
    private readonly IWorkspaceInvitationLinkFactory _workspaceInvitationLinkFactory;
    private readonly INotificationService _notificationService;
    private readonly IActivityLogService _activityLogService;

    public AddWorkspaceMemberHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceInvitationRepository workspaceInvitationRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        AddWorkspaceMemberCommandValidator validator,
        IEmailSender emailSender,
        IWorkspaceInvitationLinkFactory workspaceInvitationLinkFactory,
        INotificationService notificationService,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceInvitationRepository = workspaceInvitationRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _validator = validator;
        _emailSender = emailSender;
        _workspaceInvitationLinkFactory = workspaceInvitationLinkFactory;
        _notificationService = notificationService;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkspaceInvitationDto>> HandleAsync(
        AddWorkspaceMemberCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.InvalidAddMemberRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanManageMembers)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceManageMembersForbidden);
        }

        if (request.Role == WorkspaceRole.Owner)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.CannotAssignOwnerRole);
        }

        var normalizedEmail = WorkspaceInvitation.NormalizeEmail(request.Email);

        var targetUser = await _userRepository.GetByEmailAsync(
            normalizedEmail,
            cancellationToken);

        if (targetUser is null)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.TargetUserNotFoundByEmail(normalizedEmail));
        }

        if (!targetUser.IsActive())
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.TargetUserAccountNotActive(normalizedEmail));
        }

        if (targetUser.Id == currentUserId)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.CannotManageYourself);
        }

        var isAlreadyMember = await _workspaceMemberRepository.ExistsAsync(
            request.WorkspaceId,
            targetUser.Id,
            cancellationToken);

        if (isAlreadyMember)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceMemberAlreadyExists);
        }

        var now = _clock.UtcNow;

        var pendingInvitation = await _workspaceInvitationRepository.GetPendingByWorkspaceAndEmailAsync(
            request.WorkspaceId,
            normalizedEmail,
            now,
            cancellationToken);

        if (pendingInvitation is not null)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceInvitationAlreadyPending);
        }

        var workspace = await _workspaceRepository.GetDetailAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null)
        {
            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        var rawToken = WorkspaceInvitationToken.Create();
        var tokenHash = WorkspaceInvitationToken.Hash(rawToken);
        var expiresAtUtc = now.Add(InvitationLifetime);

        var invitation = WorkspaceInvitation.Create(
            request.WorkspaceId,
            normalizedEmail,
            request.Role,
            currentUserId,
            tokenHash,
            expiresAtUtc,
            now);

        _workspaceInvitationRepository.Add(invitation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var acceptLink = _workspaceInvitationLinkFactory.CreateAcceptLink(rawToken);
        var inviterDisplayName = _currentUser.UserName
            ?? _currentUser.Email
            ?? "Một thành viên";

        try
        {
            var email = WorkspaceInvitationEmailFactory.Create(
                invitation.Email,
                workspace.Name,
                inviterDisplayName,
                request.Role,
                acceptLink,
                expiresAtUtc);

            await _emailSender.SendAsync(email, cancellationToken);
        }
        catch
        {
            invitation.SoftDelete(_clock.UtcNow);
            _workspaceInvitationRepository.Update(invitation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<WorkspaceInvitationDto>(
                WorkspaceErrors.WorkspaceInvitationEmailFailed);
        }

        try
        {
            await _notificationService.NotifyAsync(
                targetUser.Id,
                NotificationTemplates.WorkspaceInvited(
                    actorUserId: currentUserId,
                    actorDisplayName: inviterDisplayName,
                    workspaceId: workspace.Id,
                    workspaceName: workspace.Name,
                    role: request.Role),
                cancellationToken);
        }
        catch
        {

        }

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                workspace.Id,
                currentUserId,
                ActivityAction.Assign,
                ActivityEntityType.WorkspaceMember,
                targetUser.Id,
                $"{inviterDisplayName} đã mời {targetUser.FullName} vào workspace với vai trò {request.Role}.",
                ActivityLogMetadata.Serialize(new
                {
                    invitationId = invitation.Id,
                    targetUserId = targetUser.Id,
                    targetEmail = targetUser.Email,
                    role = request.Role.ToString()
                })),
            cancellationToken);

        return Result.Success(ToDto(invitation));
    }

    private static WorkspaceInvitationDto ToDto(WorkspaceInvitation invitation)
        => new(
            invitation.Id,
            invitation.WorkspaceId,
            invitation.Email,
            invitation.Role,
            invitation.InvitedByUserId,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.CreatedDate,
            invitation.UpdatedDate);
}
