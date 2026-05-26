using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Social.Commands;

public sealed class CancelFriendRequestHandler
    : ICommandHandler<CancelFriendRequestCommand, FriendRequestDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ISocialRealtimePublisher _socialRealtimePublisher;

    public CancelFriendRequestHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ISocialRealtimePublisher socialRealtimePublisher)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _socialRealtimePublisher = socialRealtimePublisher;
    }

    public async Task<Result<FriendRequestDto>> HandleAsync(
        CancelFriendRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestDto>(SocialErrors.MissingUserContext);

        var request = await _friendshipRepository.GetRequestByIdAsync(command.RequestId, cancellationToken);
        if (request is null)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestNotFound);

        if (request.RequesterId != currentUserId)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestForbidden);

        try
        {
            request.Cancel(_clock.UtcNow);
            _friendshipRepository.UpdateRequest(request);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<FriendRequestDto>(
                new Error("Social.CancelFriendRequestFailed", ex.Message, ResultStatus.Unprocessable));
        }

        var dto = await SocialCommandHelpers.BuildFriendRequestDtoAsync(
            _userRepository,
            request,
            currentUserId,
            cancellationToken);

        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, request.AddresseeId, "FriendRequestCancelled", currentUserId, dto, cancellationToken);
        await SocialCommandHelpers.PublishFriendEventAsync(_socialRealtimePublisher, _clock, request.RequesterId, "FriendRequestCancelled", currentUserId, dto, cancellationToken);
        return Result.Success(dto);
    }
}
