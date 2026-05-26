using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed class DeleteMessageForEveryoneHandler
    : ICommandHandler<DeleteMessageForEveryoneCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    public DeleteMessageForEveryoneHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ISocialRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _messagingReadRepository = messagingReadRepository;
        _messagingWriteRepository = messagingWriteRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result> HandleAsync(
        DeleteMessageForEveryoneCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(MessagingErrors.MissingUserContext);

        if (command.MessageId == Guid.Empty)
            return Result.Failure(MessagingErrors.MessageNotFound);

        var message = await _messagingWriteRepository.GetMessageForParticipantForUpdateAsync(
            command.MessageId,
            currentUserId,
            cancellationToken);

        if (message is null)
            return Result.Failure(MessagingErrors.MessageNotFound);

        if (message.SenderUserId != currentUserId)
            return Result.Failure(MessagingErrors.MessageForbidden);

        var conversation = await _messagingWriteRepository.GetConversationForUpdateAsync(
            message.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure(MessagingErrors.ConversationForbidden);

        var now = _clock.UtcNow;
        var recipientId = conversation.GetOtherParticipant(currentUserId);

        message.DeleteForEveryone(now);
        conversation.RegisterMessage("Đã xóa một tin nhắn", now);
        _messagingWriteRepository.UpdateMessage(message);
        _messagingWriteRepository.UpdateConversation(conversation);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await MessagingCommandHelpers.InvalidateConversationListsAsync(
            _cache,
            _cacheKeyFactory,
            currentUserId,
            recipientId,
            cancellationToken);

        await MessagingCommandHelpers.PublishConversationEventAsync(
            _realtimePublisher,
            _clock,
            conversation.Id,
            currentUserId,
            recipientId,
            "MessageDeleted",
            new
            {
                messageId = command.MessageId,
                conversationId = conversation.Id,
                deletedByUserId = currentUserId
            },
            cancellationToken);

        var conversationDto = await _messagingReadRepository.GetConversationDtoAsync(
            conversation.Id,
            currentUserId,
            cancellationToken);

        if (conversationDto is not null)
        {
            await MessagingCommandHelpers.PublishConversationEventAsync(
                _realtimePublisher,
                _clock,
                conversation.Id,
                currentUserId,
                recipientId,
                "ConversationUpserted",
                conversationDto,
                cancellationToken);
        }

        return Result.Success();
    }
}
