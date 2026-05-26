using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed class UnpinMessageHandler
    : ICommandHandler<UnpinMessageCommand, MessageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    public UnpinMessageHandler(
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
        UnpinMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

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

        var existing = await _messagingWriteRepository.GetPinForMessageForUpdateAsync(
            command.MessageId,
            cancellationToken);

        if (existing is not null)
        {
            _messagingWriteRepository.RemovePin(existing);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var dto = await _messagingReadRepository.GetMessageDtoAsync(command.MessageId, currentUserId, cancellationToken)
            ?? new MessageDto(
                message.Id,
                message.ConversationId,
                message.SenderUserId,
                message.RecipientUserId,
                message.Type,
                message.Body,
                message.ImageUrl,
                message.AttachmentFileId,
                message.SenderUserId == currentUserId,
                message.ReadAtUtc,
                message.CreatedDate,
                message.UpdatedDate,
                Array.Empty<MessageReactionDto>(),
                false);

        await MessagingCommandHelpers.PublishConversationEventAsync(
            _realtimePublisher,
            _clock,
            message.ConversationId,
            currentUserId,
            conversation.GetOtherParticipant(currentUserId),
            "MessageUnpinned",
            dto,
            cancellationToken);

        return Result.Success(dto);
    }
}
