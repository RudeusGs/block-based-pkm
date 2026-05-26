using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Features.Messaging.Commands;

internal abstract class SendMessageHandlerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    protected SendMessageHandlerBase(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
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
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _realtimePublisher = realtimePublisher;
    }

    protected async Task<Result<MessageDto>> SendMessageCoreAsync(
        Guid conversationId,
        string? body,
        string? imageUrl,
        Guid? attachmentFileId,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        var conversation = await _messagingWriteRepository.GetConversationForUpdateAsync(
            conversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure<MessageDto>(MessagingErrors.ConversationForbidden);

        var recipientId = conversation.GetOtherParticipant(currentUserId);
        var now = _clock.UtcNow;
        Message message;

        try
        {
            message = imageUrl is null
                ? Message.CreateText(Guid.NewGuid(), conversation.Id, currentUserId, recipientId, body ?? string.Empty, now)
                : Message.CreateImage(Guid.NewGuid(), conversation.Id, currentUserId, recipientId, body, imageUrl, attachmentFileId, now);

            conversation.RegisterMessage(message.BuildPreview(), now);
            _messagingWriteRepository.AddMessage(message);
            _messagingWriteRepository.UpdateConversation(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<MessageDto>(
                new Error("Messaging.SendMessageFailed", ex.Message, ResultStatus.Unprocessable));
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
