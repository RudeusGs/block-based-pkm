using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed class MarkConversationReadHandler
    : ICommandHandler<MarkConversationReadCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    public MarkConversationReadHandler(
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
        MarkConversationReadCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(MessagingErrors.MissingUserContext);

        var conversation = await _messagingReadRepository.GetConversationForParticipantAsync(
            command.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure(MessagingErrors.ConversationForbidden);

        var recipientId = conversation.GetOtherParticipant(currentUserId);
        var count = await _messagingWriteRepository.MarkConversationReadAsync(
            command.ConversationId,
            currentUserId,
            _clock.UtcNow,
            cancellationToken);

        if (count > 0)
        {
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
                "ConversationRead",
                new
                {
                    conversationId = command.ConversationId,
                    readerUserId = currentUserId,
                    readCount = count
                },
                cancellationToken);
        }

        return Result.Success();
    }
}
