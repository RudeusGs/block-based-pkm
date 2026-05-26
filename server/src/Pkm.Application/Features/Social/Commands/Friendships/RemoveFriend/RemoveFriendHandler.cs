using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Social.Commands;

public sealed class RemoveFriendHandler : ICommandHandler<RemoveFriendCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISocialRealtimePublisher _socialRealtimePublisher;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public RemoveFriendHandler(
        ICurrentUser currentUser,
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        ISocialRealtimePublisher socialRealtimePublisher,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory)
    {
        _currentUser = currentUser;
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _socialRealtimePublisher = socialRealtimePublisher;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
    }

    public async Task<Result> HandleAsync(
        RemoveFriendCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(SocialErrors.MissingUserContext);

        var friendship = await _friendshipRepository.GetFriendshipAsync(
            currentUserId,
            command.FriendUserId,
            cancellationToken);

        if (friendship is null)
            return Result.Failure(SocialErrors.FriendshipNotFound);

        _friendshipRepository.RemoveFriendship(friendship);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await SocialCommandHelpers.InvalidateFriendListsAsync(
            _cache,
            _cacheKeyFactory,
            currentUserId,
            command.FriendUserId,
            cancellationToken);

        var payload = new { userId = currentUserId, friendUserId = command.FriendUserId };
        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, currentUserId, "FriendRemoved", currentUserId, payload, cancellationToken);
        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, command.FriendUserId, "FriendRemoved", currentUserId, payload, cancellationToken);

        return Result.Success();
    }
}
