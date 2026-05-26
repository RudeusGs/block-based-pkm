using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Domain.Messaging;

namespace Pkm.Application.Features.Messaging.Commands;

public sealed record CreateDirectConversationCommand(Guid RecipientUserId)
    : ICommand<ConversationDto>;

public sealed class CreateDirectConversationHandler
    : ICommandHandler<CreateDirectConversationCommand, ConversationDto>
{
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IMessagingReadRepository _messagingReadRepository;
    private readonly IMessagingWriteRepository _messagingWriteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ISocialRealtimePublisher _realtimePublisher;

    public CreateDirectConversationHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IMessagingReadRepository messagingReadRepository,
        IMessagingWriteRepository messagingWriteRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ISocialRealtimePublisher realtimePublisher)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _messagingReadRepository = messagingReadRepository;
        _messagingWriteRepository = messagingWriteRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _realtimePublisher = realtimePublisher;
    }

    public async Task<Result<ConversationDto>> HandleAsync(
        CreateDirectConversationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<ConversationDto>(MessagingErrors.MissingUserContext);

        if (command.RecipientUserId == Guid.Empty)
            return Result.Failure<ConversationDto>(MessagingErrors.RecipientNotFound);

        if (command.RecipientUserId == currentUserId)
            return Result.Failure<ConversationDto>(MessagingErrors.CannotMessageYourself);

        var recipient = await _userRepository.GetByIdAsync(command.RecipientUserId, cancellationToken);
        if (recipient is null || !recipient.IsActive())
            return Result.Failure<ConversationDto>(MessagingErrors.RecipientNotFound);

        var conversation = await _messagingReadRepository.GetDirectConversationAsync(
            currentUserId,
            command.RecipientUserId,
            cancellationToken);

        if (conversation is null)
        {
            conversation = Conversation.CreateDirect(
                Guid.NewGuid(),
                currentUserId,
                command.RecipientUserId,
                _clock.UtcNow);

            _messagingWriteRepository.AddConversation(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateConversationListsAsync(currentUserId, command.RecipientUserId, cancellationToken);
        }

        var dto = await _messagingReadRepository.GetConversationDtoAsync(
            conversation.Id,
            currentUserId,
            cancellationToken);

        if (dto is null)
            return Result.Failure<ConversationDto>(MessagingErrors.ConversationNotFound);

        await _realtimePublisher.PublishToConversationAsync(
            new MessagingRealtimeEnvelope(
                "ConversationUpserted",
                conversation.Id,
                currentUserId,
                command.RecipientUserId,
                _clock.UtcNow,
                dto),
            cancellationToken);

        return Result.Success(dto);
    }

    private async Task InvalidateConversationListsAsync(
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken)
    {
        foreach (var userId in new[] { userAId, userBId }.Where(x => x != Guid.Empty).Distinct())
        {
            await _cache.SetAsync(
                MessagingCacheKeys.ConversationListVersion(_cacheKeyFactory, userId),
                Guid.NewGuid().ToString("N"),
                VersionTtl,
                cancellationToken);
        }
    }
}
