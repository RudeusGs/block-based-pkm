using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Authorization;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.Messaging;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed class AcceptWorkspaceShareHandler
    : ICommandHandler<AcceptWorkspaceShareCommand, WorkspaceDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public AcceptWorkspaceShareHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _messagingReadRepository = messagingReadRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        AcceptWorkspaceShareCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkspaceDto>(MessagingErrors.MissingUserContext);

        var message = await _messagingReadRepository.GetMessageForRecipientAsync(
            command.MessageId,
            currentUserId,
            cancellationToken);

        if (message is null || message.Type != MessageType.WorkspaceShare)
            return Result.Failure<WorkspaceDto>(MessagingErrors.ConversationForbidden);

        var payload = WorkspaceShareCommandHelpers.DeserializePayload(message.Body);
        if (payload is null || payload.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDto>(MessagingErrors.InvalidRequest(new[]
            {
                "Tin nhắn chia sẻ workspace không còn hợp lệ."
            }));
        }

        if (!WorkspaceShareCommandHelpers.TryParseShareRole(payload.GrantedRole, out var grantedRole))
        {
            return Result.Failure<WorkspaceDto>(MessagingErrors.InvalidRequest(new[]
            {
                "Quyền workspace được chia sẻ không hợp lệ."
            }));
        }

        var sharerAccess = await _workspaceAccessEvaluator.EvaluateAsync(
            payload.WorkspaceId,
            message.SenderUserId,
            cancellationToken);

        if (!sharerAccess.Exists)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!sharerAccess.CanManageMembers)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceManageMembersForbidden);

        var workspace = await _workspaceRepository.GetDetailAsync(payload.WorkspaceId, cancellationToken);
        if (workspace is null)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);

        var existingMember = await _workspaceMemberRepository.GetByWorkspaceAndUserAsync(
            payload.WorkspaceId,
            currentUserId,
            cancellationToken);

        var effectiveRole = existingMember?.Role ?? grantedRole;

        if (existingMember is null)
        {
            var now = _clock.UtcNow;
            var member = grantedRole == WorkspaceRole.Member
                ? WorkspaceMember.CreateMember(payload.WorkspaceId, currentUserId, now)
                : WorkspaceMember.CreateViewer(payload.WorkspaceId, currentUserId, now);

            _workspaceMemberRepository.Add(member);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await WorkspaceShareCommandHelpers.InvalidateWorkspaceMembershipCachesAsync(
                _cache,
                _cacheKeyFactory,
                payload.WorkspaceId,
                currentUserId,
                cancellationToken);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    payload.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.WorkspaceMember,
                    currentUserId,
                    $"{_currentUser.UserName ?? "Có người"} đã tham gia workspace qua Messenger share.",
                    ActivityLogMetadata.Serialize(new
                    {
                        messageId = command.MessageId,
                        sharedByUserId = message.SenderUserId,
                        role = grantedRole.ToString()
                    })),
                cancellationToken);
        }

        var capabilities = WorkspaceRoleCapabilityMatrix.ForWorkspace(false, effectiveRole);
        var dto = new WorkspaceDto(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.AvatarUrl,
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
}
