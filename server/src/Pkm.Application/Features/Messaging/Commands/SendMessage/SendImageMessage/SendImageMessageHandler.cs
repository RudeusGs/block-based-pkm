using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Files.Models;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Notifications.Services;

namespace Pkm.Application.Features.Messaging.Commands;

internal sealed class SendImageMessageHandler
    : SendMessageHandlerBase, ICommandHandler<SendImageMessageCommand, MessageDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;

    public SendImageMessageHandler(
        ICurrentUser currentUser,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ISocialRealtimePublisher realtimePublisher,
        IFileUploadApplicationService fileUploadApplicationService)
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
        _currentUser = currentUser;
        _fileUploadApplicationService = fileUploadApplicationService;
    }

    public async Task<Result<MessageDto>> HandleAsync(
        SendImageMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<MessageDto>(MessagingErrors.MissingUserContext);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(
                currentUserId,
                command.FileName,
                command.ContentType,
                command.SizeBytes,
                command.Content,
                "message-image"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<MessageDto>(uploadResult.Error);

        return await SendMessageCoreAsync(
            command.ConversationId,
            command.Caption,
            uploadResult.Value.PublicUrl,
            uploadResult.Value.Id,
            cancellationToken);
    }
}
