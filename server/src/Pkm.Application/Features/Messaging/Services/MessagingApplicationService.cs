using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using System.Text.Json;
using Pkm.Application.Common.Authorization;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.Common;
using Pkm.Domain.Messaging;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Messaging.Services;

public sealed class MessagingApplicationService : IMessagingApplicationService
{
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);
    private static readonly JsonSerializerOptions WorkspaceShareJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IMessagingRepository _messagingRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;
    private readonly ISocialRealtimePublisher _realtimePublisher;
    private readonly INotificationService _notificationService;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public MessagingApplicationService(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IMessagingRepository messagingRepository,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IFileUploadApplicationService fileUploadApplicationService,
        ISocialRealtimePublisher realtimePublisher,
        INotificationService notificationService,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _messagingRepository = messagingRepository;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _fileUploadApplicationService = fileUploadApplicationService;
        _realtimePublisher = realtimePublisher;
        _notificationService = notificationService;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result<ConversationDto>> CreateOrGetDirectConversationAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<ConversationDto>(MessagingErrors.MissingUserContext);

        if (recipientUserId == Guid.Empty)
            return Result.Failure<ConversationDto>(MessagingErrors.RecipientNotFound);

        if (recipientUserId == currentUserId)
            return Result.Failure<ConversationDto>(MessagingErrors.CannotMessageYourself);

        var recipient = await _userRepository.GetByIdAsync(recipientUserId, cancellationToken);
        if (recipient is null || !recipient.IsActive())
            return Result.Failure<ConversationDto>(MessagingErrors.RecipientNotFound);

        if (!await _friendshipRepository.AreFriendsAsync(currentUserId, recipientUserId, cancellationToken))
            return Result.Failure<ConversationDto>(MessagingErrors.FriendshipRequired);

        var existing = await _messagingRepository.GetDirectConversationAsync(currentUserId, recipientUserId, cancellationToken);
        if (existing is null)
        {
            existing = Conversation.CreateDirect(Guid.NewGuid(), currentUserId, recipientUserId, _clock.UtcNow);
            _messagingRepository.AddConversation(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateConversationListsAsync(currentUserId, recipientUserId, cancellationToken);
        }

        var dto = await _messagingRepository.GetConversationDtoAsync(
            existing.Id,
            currentUserId,
            cancellationToken);

        if (dto is null)
            return Result.Failure<ConversationDto>(MessagingErrors.ConversationNotFound);

        await PublishConversationEventAsync(existing.Id, currentUserId, recipientUserId, "ConversationUpserted", dto, cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<ConversationPagedResultDto>> ListConversationsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<ConversationPagedResultDto>(MessagingErrors.MissingUserContext);

        var page = NormalizePage(pageNumber);
        var size = NormalizeSize(pageSize);
        var version = await _redisCache.GetAsync<string>(MessagingCacheKeys.ConversationListVersion(_redisKeyFactory, currentUserId), cancellationToken) ?? "1";
        var cacheKey = MessagingCacheKeys.ConversationList(_redisKeyFactory, currentUserId, page, size, version);
        var cached = await _redisCache.GetAsync<ConversationPagedResultDto>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var items = await _messagingRepository.ListConversationsAsync(currentUserId, page, size, cancellationToken);
        var total = await _messagingRepository.CountConversationsAsync(currentUserId, cancellationToken);
        var dto = new ConversationPagedResultDto(items, page, size, total, CalculateTotalPages(total, size));
        await _redisCache.SetAsync(cacheKey, dto, ListCacheTtl, cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<MessagePagedResultDto>> ListMessagesAsync(Guid conversationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessagePagedResultDto>(MessagingErrors.MissingUserContext);

        var conversation = await _messagingRepository.GetConversationForParticipantAsync(conversationId, currentUserId, cancellationToken);
        if (conversation is null)
            return Result.Failure<MessagePagedResultDto>(MessagingErrors.ConversationForbidden);

        var page = NormalizePage(pageNumber);
        var size = NormalizeSize(pageSize);
        var messages = await _messagingRepository.ListMessagesAsync(conversationId, currentUserId, page, size, cancellationToken);
        var total = await _messagingRepository.CountMessagesAsync(conversationId, cancellationToken);
        return Result.Success(new MessagePagedResultDto(messages, page, size, total, CalculateTotalPages(total, size)));
    }

    public async Task<Result<MessageDto>> SendTextMessageAsync(Guid conversationId, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Result.Failure<MessageDto>(MessagingErrors.EmptyMessage);

        return await SendMessageCoreAsync(conversationId, body, null, null, cancellationToken);
    }

    public async Task<Result<MessageDto>> SendImageMessageAsync(
        Guid conversationId,
        string? caption,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(currentUserId, fileName, contentType, sizeBytes, content, "message-image"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<MessageDto>(uploadResult.Error);

        return await SendMessageCoreAsync(conversationId, caption, uploadResult.Value.PublicUrl, uploadResult.Value.Id, cancellationToken);
    }

    public async Task<Result<MessageDto>> SendWorkspaceShareMessageAsync(
        Guid conversationId,
        Guid workspaceId,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        if (!TryParseShareRole(role, out var grantedRole))
        {
            return Result.Failure<MessageDto>(MessagingErrors.InvalidRequest(new[]
            {
                "Role chia sẻ không hợp lệ. Giá trị hợp lệ: viewer, member."
            }));
        }

        var conversation = await _messagingRepository.GetConversationForUpdateAsync(
            conversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure<MessageDto>(MessagingErrors.ConversationForbidden);

        var recipientId = conversation.GetOtherParticipant(currentUserId);

        if (!await _friendshipRepository.AreFriendsAsync(currentUserId, recipientId, cancellationToken))
            return Result.Failure<MessageDto>(MessagingErrors.FriendshipRequired);

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            workspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanManageMembers)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceManageMembersForbidden);

        var recipientAlreadyMember = await _workspaceMemberRepository.ExistsAsync(
            workspaceId,
            recipientId,
            cancellationToken);

        if (recipientAlreadyMember)
            return Result.Failure<MessageDto>(WorkspaceErrors.WorkspaceMemberAlreadyExists);

        var workspace = await _workspaceRepository.GetDetailAsync(workspaceId, cancellationToken);
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

        var payloadJson = JsonSerializer.Serialize(payload, WorkspaceShareJsonOptions);
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
            _messagingRepository.AddMessage(message);
            _messagingRepository.UpdateConversation(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<MessageDto>(new Error("Messaging.ShareWorkspaceFailed", ex.Message, ResultStatus.Unprocessable));
        }

        await InvalidateConversationListsAsync(currentUserId, recipientId, cancellationToken);

        var dto = await _messagingRepository.GetMessageDtoAsync(message.Id, currentUserId, cancellationToken)
            ?? new MessageDto(message.Id, message.ConversationId, message.SenderUserId, message.RecipientUserId, message.Type, message.Body, message.ImageUrl, message.AttachmentFileId, true, message.ReadAtUtc, message.CreatedDate, message.UpdatedDate);

        await PublishConversationEventAsync(conversation.Id, currentUserId, recipientId, "MessageCreated", dto, cancellationToken);

        await _notificationService.NotifyAsync(
            recipientId,
            NotificationTemplates.MessageReceived(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                conversation.Id),
            cancellationToken);

        return Result.Success(dto);
    }

    public async Task<Result<WorkspaceDto>> AcceptWorkspaceShareAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkspaceDto>(MessagingErrors.MissingUserContext);

        var message = await _messagingRepository.GetMessageForRecipientAsync(
            messageId,
            currentUserId,
            cancellationToken);

        if (message is null || message.Type != MessageType.WorkspaceShare)
            return Result.Failure<WorkspaceDto>(MessagingErrors.ConversationForbidden);

        var payload = DeserializeWorkspaceSharePayload(message.Body);
        if (payload is null || payload.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDto>(MessagingErrors.InvalidRequest(new[]
            {
                "Tin nhắn chia sẻ workspace không còn hợp lệ."
            }));
        }

        if (!TryParseShareRole(payload.GrantedRole, out var grantedRole))
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

            await InvalidateWorkspaceMembershipCachesAsync(
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
                        messageId,
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

    public async Task<Result> MarkConversationReadAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(MessagingErrors.MissingUserContext);

        var conversation = await _messagingRepository.GetConversationForParticipantAsync(conversationId, currentUserId, cancellationToken);
        if (conversation is null)
            return Result.Failure(MessagingErrors.ConversationForbidden);

        var count = await _messagingRepository.MarkConversationReadAsync(conversationId, currentUserId, _clock.UtcNow, cancellationToken);
        if (count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateConversationListsAsync(currentUserId, conversation.GetOtherParticipant(currentUserId), cancellationToken);
            await PublishConversationEventAsync(
                conversation.Id,
                currentUserId,
                conversation.GetOtherParticipant(currentUserId),
                "ConversationRead",
                new { conversationId, readerUserId = currentUserId, readCount = count },
                cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result<MessageDto>> SendMessageCoreAsync(
        Guid conversationId,
        string? body,
        string? imageUrl,
        Guid? attachmentFileId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        var conversation = await _messagingRepository.GetConversationForUpdateAsync(conversationId, currentUserId, cancellationToken);
        if (conversation is null)
            return Result.Failure<MessageDto>(MessagingErrors.ConversationForbidden);

        var recipientId = conversation.GetOtherParticipant(currentUserId);

        if (!await _friendshipRepository.AreFriendsAsync(currentUserId, recipientId, cancellationToken))
            return Result.Failure<MessageDto>(MessagingErrors.FriendshipRequired);

        var now = _clock.UtcNow;
        Message message;

        try
        {
            message = imageUrl is null
                ? Message.CreateText(Guid.NewGuid(), conversation.Id, currentUserId, recipientId, body ?? string.Empty, now)
                : Message.CreateImage(Guid.NewGuid(), conversation.Id, currentUserId, recipientId, body, imageUrl, attachmentFileId, now);

            conversation.RegisterMessage(message.BuildPreview(), now);
            _messagingRepository.AddMessage(message);
            _messagingRepository.UpdateConversation(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<MessageDto>(new Error("Messaging.SendMessageFailed", ex.Message, ResultStatus.Unprocessable));
        }

        await InvalidateConversationListsAsync(currentUserId, recipientId, cancellationToken);

        var dto = await _messagingRepository.GetMessageDtoAsync(message.Id, currentUserId, cancellationToken)
            ?? new MessageDto(message.Id, message.ConversationId, message.SenderUserId, message.RecipientUserId, message.Type, message.Body, message.ImageUrl, message.AttachmentFileId, true, message.ReadAtUtc, message.CreatedDate, message.UpdatedDate);

        await PublishConversationEventAsync(conversation.Id, currentUserId, recipientId, "MessageCreated", dto, cancellationToken);

        await _notificationService.NotifyAsync(
            recipientId,
            NotificationTemplates.MessageReceived(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                conversation.Id),
            cancellationToken);

        return Result.Success(dto);
    }

    private static WorkspaceSharePayloadDto? DeserializeWorkspaceSharePayload(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            return JsonSerializer.Deserialize<WorkspaceSharePayloadDto>(body, WorkspaceShareJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryParseShareRole(string? rawRole, out WorkspaceRole role)
    {
        role = WorkspaceRole.Viewer;

        if (string.IsNullOrWhiteSpace(rawRole))
            return true;

        switch (rawRole.Trim().ToLowerInvariant())
        {
            case "member":
                role = WorkspaceRole.Member;
                return true;

            case "viewer":
                role = WorkspaceRole.Viewer;
                return true;

            default:
                return false;
        }
    }

    private async Task InvalidateWorkspaceMembershipCachesAsync(
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

    private async Task InvalidateConversationListsAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken)
    {
        foreach (var userId in new[] { userAId, userBId }.Where(x => x != Guid.Empty).Distinct())
        {
            await _redisCache.SetAsync(
                MessagingCacheKeys.ConversationListVersion(_redisKeyFactory, userId),
                Guid.NewGuid().ToString("N"),
                VersionTtl,
                cancellationToken);
        }
    }

    private async Task PublishConversationEventAsync(Guid conversationId, Guid senderId, Guid recipientId, string eventName, object payload, CancellationToken cancellationToken)
    {
        await _realtimePublisher.PublishToConversationAsync(
            new MessagingRealtimeEnvelope(eventName, conversationId, senderId, recipientId, _clock.UtcNow, payload),
            cancellationToken);
    }

    private static int NormalizePage(int pageNumber) => pageNumber <= 0 ? 1 : pageNumber;
    private static int NormalizeSize(int pageSize) => pageSize <= 0 ? 30 : Math.Min(pageSize, 100);
    private static int CalculateTotalPages(int total, int size) => total <= 0 ? 0 : (int)Math.Ceiling(total / (double)size);
}

