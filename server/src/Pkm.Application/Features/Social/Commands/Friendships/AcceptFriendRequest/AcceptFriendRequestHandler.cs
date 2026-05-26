using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Social;

namespace Pkm.Application.Features.Social.Commands;

public sealed class AcceptFriendRequestHandler
    : ICommandHandler<AcceptFriendRequestCommand, FriendRequestDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly ISocialRealtimePublisher _socialRealtimePublisher;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public AcceptFriendRequestHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        ISocialRealtimePublisher socialRealtimePublisher,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _socialRealtimePublisher = socialRealtimePublisher;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public async Task<Result<FriendRequestDto>> HandleAsync(
        AcceptFriendRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestDto>(SocialErrors.MissingUserContext);

        var request = await _friendshipRepository.GetRequestByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestNotFound);

        if (request.AddresseeId != currentUserId)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestForbidden);

        if (!request.IsPending)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestAlreadyPending);

        var now = _clock.UtcNow;

        try
        {
            request.Accept(now);

            if (!await _friendshipRepository.AreFriendsAsync(request.RequesterId, request.AddresseeId, cancellationToken))
            {
                _friendshipRepository.AddFriendship(
                    Friendship.Create(Guid.NewGuid(), request.RequesterId, request.AddresseeId, now));
            }

            _friendshipRepository.UpdateRequest(request);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<FriendRequestDto>(
                new Error("Social.AcceptFriendRequestFailed", ex.Message, ResultStatus.Unprocessable));
        }

        await SocialCommandHelpers.InvalidateFriendListsAsync(
            _cache,
            _cacheKeyFactory,
            request.RequesterId,
            request.AddresseeId,
            cancellationToken);

        var dto = await SocialCommandHelpers.BuildFriendRequestDtoAsync(
            _userRepository,
            request,
            currentUserId,
            cancellationToken);

        await _notificationService.NotifyAsync(
            request.RequesterId,
            NotificationTemplates.FriendRequestAccepted(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                request.Id),
            cancellationToken);

        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, request.RequesterId, "FriendRequestAccepted", currentUserId, dto, cancellationToken);
        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, request.AddresseeId, "FriendRequestAccepted", currentUserId, dto, cancellationToken);
        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, request.RequesterId, "FriendshipChanged", currentUserId, dto, cancellationToken);
        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, request.AddresseeId, "FriendshipChanged", currentUserId, dto, cancellationToken);

        return Result.Success(dto);
    }
}
