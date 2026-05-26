using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Notifications.Services;

namespace Pkm.Application.Features.Messaging.Commands;

internal sealed class SendTextMessageHandler
    : SendMessageHandlerBase, ICommandHandler<SendTextMessageCommand, MessageDto>
{
    public SendTextMessageHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ISocialRealtimePublisher realtimePublisher)
        : base(
            currentUser,
            messagingReadRepository,
            messagingWriteRepository,
            unitOfWork,
            clock,
            notificationService,
            cache,
            cacheKeyFactory,
            realtimePublisher)
    {
    }

    public async Task<Result<MessageDto>> HandleAsync(
        SendTextMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Body))
            return Result.Failure<MessageDto>(MessagingErrors.EmptyMessage);

        return await SendMessageCoreAsync(
            command.ConversationId,
            command.Body,
            imageUrl: null,
            attachmentFileId: null,
            cancellationToken);
    }
}
