using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Domain.Common;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Features.Messaging.Services;

public sealed class MessagingApplicationService : IMessagingApplicationService
{
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IMessagingRepository _messagingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;
    private readonly ISocialRealtimePublisher _realtimePublisher;
    private readonly INotificationService _notificationService;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public MessagingApplicationService(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IMessagingRepository messagingRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IFileUploadApplicationService fileUploadApplicationService,
        ISocialRealtimePublisher realtimePublisher,
        INotificationService notificationService,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _messagingRepository = messagingRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _fileUploadApplicationService = fileUploadApplicationService;
        _realtimePublisher = realtimePublisher;
        _notificationService = notificationService;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
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
