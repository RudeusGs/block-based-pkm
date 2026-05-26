using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed class ToggleMessageReactionHandler
    : ICommandHandler<ToggleMessageReactionCommand, MessageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    public ToggleMessageReactionHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ISocialRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _messagingReadRepository = messagingReadRepository;
        _messagingWriteRepository = messagingWriteRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result<MessageDto>> HandleAsync(
        ToggleMessageReactionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        var normalizedEmoji = command.Emoji?.Trim() ?? string.Empty;
        if (command.MessageId == Guid.Empty ||
            string.IsNullOrWhiteSpace(normalizedEmoji) ||
            normalizedEmoji.Length > MessageReaction.MaxEmojiLength)
        {
            return Result.Failure<MessageDto>(MessagingErrors.InvalidRequest(new[]
            {
                "Reaction không hợp lệ."
            }));
        }

        var message = await _messagingWriteRepository.GetMessageForParticipantForUpdateAsync(
            command.MessageId,
            currentUserId,
            cancellationToken);

        if (message is null)
            return Result.Failure<MessageDto>(MessagingErrors.MessageNotFound);

        var conversation = await _messagingReadRepository.GetConversationForParticipantAsync(
            message.ConversationId,
            currentUserId,
            cancellationToken);

        if (conversation is null)
            return Result.Failure<MessageDto>(MessagingErrors.ConversationForbidden);

        var now = _clock.UtcNow;
        var existing = await _messagingWriteRepository.GetReactionForUserForUpdateAsync(
            command.MessageId,
            currentUserId,
            cancellationToken);

        try
        {
            if (existing is null)
            {
                _messagingWriteRepository.AddReaction(MessageReaction.Create(
                    Guid.NewGuid(),
                    command.MessageId,
                    currentUserId,
                    normalizedEmoji,
                    now));
            }
            else if (string.Equals(existing.Emoji, normalizedEmoji, StringComparison.Ordinal))
            {
                _messagingWriteRepository.RemoveReaction(existing);
            }
            else
            {
                existing.ChangeEmoji(normalizedEmoji, now);
                _messagingWriteRepository.UpdateReaction(existing);
            }
        }
        catch (DomainException ex)
        {
            return Result.Failure<MessageDto>(
                new Error("Messaging.InvalidReaction", ex.Message, ResultStatus.Validation));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await _messagingReadRepository.GetMessageDtoAsync(
            command.MessageId,
            currentUserId,
            cancellationToken);

        if (dto is null)
            return Result.Failure<MessageDto>(MessagingErrors.MessageNotFound);

        await MessagingCommandHelpers.PublishConversationEventAsync(
            _realtimePublisher,
            _clock,
            message.ConversationId,
            currentUserId,
            conversation.GetOtherParticipant(currentUserId),
            "MessageReactionChanged",
            dto,
            cancellationToken);

        return Result.Success(dto);
    }
}
