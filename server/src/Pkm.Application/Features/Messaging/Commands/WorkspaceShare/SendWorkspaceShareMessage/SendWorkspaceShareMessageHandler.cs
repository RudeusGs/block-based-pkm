using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed class SendWorkspaceShareMessageHandler
    : ICommandHandler<SendWorkspaceShareMessageCommand, MessageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    public SendWorkspaceShareMessageHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ISocialRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _messagingReadRepository = messagingReadRepository;
        _messagingWriteRepository = messagingWriteRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result<MessageDto>> HandleAsync(
        SendWorkspaceShareMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        if (!WorkspaceShareCommandHelpers.TryParseShareRole(command.Role, out var grantedRole))
        {
            return Result.Failure<MessageDto>(MessagingErrors.InvalidRequest(new[]
            {
                "Role chia sẻ không hợp lệ. Giá trị hợp lệ: viewer, member."
            }));
        }

        var conversation = await _messagingWriteRepository.GetConversationForUpdateAsync(
            command.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure<MessageDto>(MessagingErrors.ConversationForbidden);

        var recipientId = conversation.GetOtherParticipant(currentUserId);

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            command.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanManageMembers)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceManageMembersForbidden);

        var recipientAlreadyMember = await _workspaceMemberRepository.ExistsAsync(
            command.WorkspaceId,
            recipientId,
            cancellationToken);

        if (recipientAlreadyMember)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceMemberAlreadyExists);

        var workspace = await _workspaceRepository.GetDetailAsync(command.WorkspaceId, cancellationToken);
        if (workspace is null)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceNotFound);

        var now = _clock.UtcNow;
        var payload = new WorkspaceSharePayloadDto(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.Visibility.ToString(),
            grantedRole.ToString(),
            currentUserId,
            _currentUser.UserName ?? _currentUser.Email ?? "Một thành viên",
            now);

        var payloadJson = WorkspaceShareCommandHelpers.SerializePayload(payload);
        Message message;

        try
        {
            message = Message.CreateWorkspaceShare(
                Guid.NewGuid(),
                conversation.Id,
                currentUserId,
                recipientId,
                payloadJson,
                now);

            conversation.RegisterMessage($"Đã chia sẻ workspace {workspace.Name}", now);
            _messagingWriteRepository.AddMessage(message);
            _messagingWriteRepository.UpdateConversation(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<MessageDto>(
                new Error("Messaging.ShareWorkspaceFailed", ex.Message, ResultStatus.Unprocessable));
        }

        await MessagingCommandHelpers.InvalidateConversationListsAsync(
            _cache,
            _cacheKeyFactory,
            currentUserId,
            recipientId,
            cancellationToken);

        var dto = await _messagingReadRepository.GetMessageDtoAsync(message.Id, currentUserId, cancellationToken)
            ?? new MessageDto(
                message.Id,
                message.ConversationId,
                message.SenderUserId,
                message.RecipientUserId,
                message.Type,
                message.Body,
                message.ImageUrl,
                message.AttachmentFileId,
                true,
                message.ReadAtUtc,
                message.CreatedDate,
                message.UpdatedDate,
                Array.Empty<MessageReactionDto>(),
                false);

        await MessagingCommandHelpers.PublishConversationEventAsync(
            _realtimePublisher,
            _clock,
            conversation.Id,
            currentUserId,
            recipientId,
            "MessageCreated",
            dto,
            cancellationToken);

        await _notificationService.NotifyAsync(
            recipientId,
            NotificationTemplates.MessageReceived(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                conversation.Id),
            cancellationToken);

        return Result.Success(dto);
    }
}
