using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Files.Models;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Common;
using Pkm.Domain.Profiles;
using Pkm.Domain.Social;
using Pkm.Domain.Users;

namespace Pkm.Application.Features.Social.Services;

public sealed class SocialApplicationService : ISocialApplicationService
{
    private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan ListCacheTtl = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    private readonly ISocialRealtimePublisher _socialRealtimePublisher;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;

    public SocialApplicationService(
        ICurrentUser currentUser,
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IUserProfileRepository profileRepository,
        IWorkspaceRepository workspaceRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService,
        ISocialRealtimePublisher socialRealtimePublisher,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IFileUploadApplicationService fileUploadApplicationService)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _profileRepository = profileRepository;
        _workspaceRepository = workspaceRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
        _socialRealtimePublisher = socialRealtimePublisher;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _fileUploadApplicationService = fileUploadApplicationService;
    }

    public async Task<Result<IReadOnlyList<UserSearchResultDto>>> SearchUsersAsync(
        string keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<IReadOnlyList<UserSearchResultDto>>(SocialErrors.MissingUserContext);

        var normalizedKeyword = (keyword ?? string.Empty).Trim();
        if (normalizedKeyword.Length < 2)
            return Result.Failure<IReadOnlyList<UserSearchResultDto>>(SocialErrors.InvalidRequest(new[] { "Từ khóa tìm kiếm phải có ít nhất 2 ký tự." }));

        var safePage = NormalizePage(pageNumber);
        var safeSize = NormalizeSize(pageSize);
        var cacheKey = SocialCacheKeys.UserSearch(_redisKeyFactory, currentUserId, normalizedKeyword, safePage, safeSize);
        var cached = await _redisCache.GetAsync<IReadOnlyList<UserSearchResultDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var items = await _friendshipRepository.SearchUsersAsync(
            currentUserId,
            normalizedKeyword,
            safePage,
            safeSize,
            cancellationToken);

        await _redisCache.SetAsync(cacheKey, items, SearchCacheTtl, cancellationToken);
        return Result.Success(items);
    }

    public async Task<Result<FriendRequestDto>> SendFriendRequestAsync(
        Guid addresseeUserId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestDto>(SocialErrors.MissingUserContext);

        if (addresseeUserId == Guid.Empty)
            return Result.Failure<FriendRequestDto>(SocialErrors.UserNotFound);

        if (addresseeUserId == currentUserId)
            return Result.Failure<FriendRequestDto>(SocialErrors.CannotFriendYourself);

        var addressee = await _userRepository.GetByIdAsync(addresseeUserId, cancellationToken);
        if (addressee is null || !addressee.IsActive())
            return Result.Failure<FriendRequestDto>(SocialErrors.UserNotFound);

        if (await _friendshipRepository.AreFriendsAsync(currentUserId, addresseeUserId, cancellationToken))
            return Result.Failure<FriendRequestDto>(SocialErrors.AlreadyFriends);

        var reversePending = await _friendshipRepository.GetPendingRequestAsync(
            addresseeUserId,
            currentUserId,
            cancellationToken);

        if (reversePending is not null)
            return await AcceptFriendRequestAsync(reversePending.Id, cancellationToken);

        var existingPending = await _friendshipRepository.GetPendingRequestAsync(
            currentUserId,
            addresseeUserId,
            cancellationToken);

        if (existingPending is not null)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestAlreadyPending);

        var now = _clock.UtcNow;
        var request = FriendRequest.Create(Guid.NewGuid(), currentUserId, addresseeUserId, now);
        _friendshipRepository.AddRequest(request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await BuildFriendRequestDtoAsync(request, currentUserId, cancellationToken);

        await _notificationService.NotifyAsync(
            addresseeUserId,
            NotificationTemplates.FriendRequestReceived(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                request.Id),
            cancellationToken);

        await PublishFriendEventAsync(addresseeUserId, "FriendRequestReceived", currentUserId, dto, cancellationToken);
        await PublishFriendEventAsync(currentUserId, "FriendRequestSent", currentUserId, dto, cancellationToken);

        return Result.Success(dto);
    }

    public async Task<Result<FriendRequestDto>> AcceptFriendRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestDto>(SocialErrors.MissingUserContext);

        var request = await _friendshipRepository.GetRequestByIdAsync(requestId, cancellationToken);
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
            return Result.Failure<FriendRequestDto>(new Error("Social.AcceptFriendRequestFailed", ex.Message, ResultStatus.Unprocessable));
        }

        await InvalidateFriendListsAsync(request.RequesterId, request.AddresseeId, cancellationToken);

        var dto = await BuildFriendRequestDtoAsync(request, currentUserId, cancellationToken);

        await _notificationService.NotifyAsync(
            request.RequesterId,
            NotificationTemplates.FriendRequestAccepted(
                currentUserId,
                _currentUser.UserName ?? "Có người",
                request.Id),
            cancellationToken);

        await PublishFriendEventAsync(request.RequesterId, "FriendRequestAccepted", currentUserId, dto, cancellationToken);
        await PublishFriendEventAsync(request.AddresseeId, "FriendRequestAccepted", currentUserId, dto, cancellationToken);
        await PublishFriendEventAsync(request.RequesterId, "FriendshipChanged", currentUserId, dto, cancellationToken);
        await PublishFriendEventAsync(request.AddresseeId, "FriendshipChanged", currentUserId, dto, cancellationToken);

        return Result.Success(dto);
    }

    public async Task<Result<FriendRequestDto>> RejectFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestDto>(SocialErrors.MissingUserContext);

        var request = await _friendshipRepository.GetRequestByIdAsync(requestId, cancellationToken);
        if (request is null)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestNotFound);

        if (request.AddresseeId != currentUserId)
            return Result.Failure<FriendRequestDto>(SocialErrors.FriendRequestForbidden);

        try
        {
            request.Reject(_clock.UtcNow);
            _friendshipRepository.UpdateRequest(request);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<FriendRequestDto>(new Error("Social.RejectFriendRequestFailed", ex.Message, ResultStatus.Unprocessable));
        }

        var dto = await BuildFriendRequestDtoAsync(request, currentUserId, cancellationToken);
        await PublishFriendEventAsync(request.RequesterId, "FriendRequestRejected", currentUserId, dto, cancellationToken);
        await PublishFriendEventAsync(request.AddresseeId, "FriendRequestRejected", currentUserId, dto, cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result<FriendRequestDto>> CancelFriendRequestAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<FriendRequestDto>(SocialErrors.MissingUserContext);

        var request = await _friendshipRepository.GetRequestByIdAsync(requestId, cancellationToken);
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
            return Result.Failure<FriendRequestDto>(new Error("Social.CancelFriendRequestFailed", ex.Message, ResultStatus.Unprocessable));
        }

        var dto = await BuildFriendRequestDtoAsync(request, currentUserId, cancellationToken);
        await PublishFriendEventAsync(request.AddresseeId, "FriendRequestCancelled", currentUserId, dto, cancellationToken);
        await PublishFriendEventAsync(request.RequesterId, "FriendRequestCancelled", currentUserId, dto, cancellationToken);
        return Result.Success(dto);
    }

    public async Task<Result> RemoveFriendAsync(Guid friendUserId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure(SocialErrors.MissingUserContext);

        var friendship = await _friendshipRepository.GetFriendshipAsync(currentUserId, friendUserId, cancellationToken);
        if (friendship is null)
            return Result.Failure(SocialErrors.FriendshipNotFound);

        _friendshipRepository.RemoveFriendship(friendship);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await InvalidateFriendListsAsync(currentUserId, friendUserId, cancellationToken);

        var payload = new { userId = currentUserId, friendUserId };
        await PublishFriendEventAsync(currentUserId, "FriendRemoved", currentUserId, payload, cancellationToken);
        await PublishFriendEventAsync(friendUserId, "FriendRemoved", currentUserId, payload, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<FriendDto>>> ListFriendsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<IReadOnlyList<FriendDto>>(SocialErrors.MissingUserContext);

        var page = NormalizePage(pageNumber);
        var size = NormalizeSize(pageSize);
        var version = await _redisCache.GetAsync<string>(SocialCacheKeys.FriendListVersion(_redisKeyFactory, currentUserId), cancellationToken) ?? "1";
        var cacheKey = SocialCacheKeys.FriendList(_redisKeyFactory, currentUserId, page, size, version);
        var cached = await _redisCache.GetAsync<IReadOnlyList<FriendDto>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result.Success(cached);

        var friends = await _friendshipRepository.ListFriendsAsync(currentUserId, page, size, cancellationToken);
        await _redisCache.SetAsync(cacheKey, friends, ListCacheTtl, cancellationToken);
        return Result.Success(friends);
    }

    public async Task<Result<IReadOnlyList<FriendRequestDto>>> ListIncomingRequestsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<IReadOnlyList<FriendRequestDto>>(SocialErrors.MissingUserContext);

        var result = await _friendshipRepository.ListIncomingRequestsAsync(currentUserId, NormalizePage(pageNumber), NormalizeSize(pageSize), cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<IReadOnlyList<FriendRequestDto>>> ListOutgoingRequestsAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<IReadOnlyList<FriendRequestDto>>(SocialErrors.MissingUserContext);

        var result = await _friendshipRepository.ListOutgoingRequestsAsync(currentUserId, NormalizePage(pageNumber), NormalizeSize(pageSize), cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<UserProfilePageDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfilePageDto>(SocialErrors.MissingUserContext);

        var target = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (target is null)
            return Result.Failure<UserProfilePageDto>(SocialErrors.UserNotFound);

        var workspaces = await _workspaceRepository.ListProfileWorkspacesAsync(
            userId,
            currentUserId,
            includePrivate: userId == currentUserId,
            cancellationToken);

        var profile = await _profileRepository.GetProfileAsync(userId, currentUserId, workspaces, cancellationToken);
        if (profile is null)
            return Result.Failure<UserProfilePageDto>(SocialErrors.UserNotFound);

        return Result.Success(profile);
    }

    public async Task<Result<UserProfilePageDto>> UpdateMyProfilePageAsync(string? bio, string? coverImageUrl, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfilePageDto>(SocialErrors.MissingUserContext);

        var profile = await EnsureProfileForUpdateAsync(currentUserId, cancellationToken);

        try
        {
            profile.Update(bio, coverImageUrl, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserProfilePageDto>(new Error("Social.UpdateProfilePageFailed", ex.Message, ResultStatus.Unprocessable));
        }

        return await GetProfileAsync(currentUserId, cancellationToken);
    }

    public async Task<Result<UserProfilePageDto>> UploadMyProfileCoverImageAsync(
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<UserProfilePageDto>(SocialErrors.MissingUserContext);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(currentUserId, fileName, contentType, sizeBytes, content, "profile-cover"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<UserProfilePageDto>(uploadResult.Error);

        var profile = await EnsureProfileForUpdateAsync(currentUserId, cancellationToken);

        try
        {
            profile.SetCoverImage(uploadResult.Value.PublicUrl, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DomainException ex)
        {
            return Result.Failure<UserProfilePageDto>(new Error("Social.UploadProfileCoverFailed", ex.Message, ResultStatus.Unprocessable));
        }

        return await GetProfileAsync(currentUserId, cancellationToken);
    }

    private async Task<UserProfilePage> EnsureProfileForUpdateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdForUpdateAsync(userId, cancellationToken);
        if (profile is not null)
            return profile;

        profile = UserProfilePage.Create(Guid.NewGuid(), userId, _clock.UtcNow);
        _profileRepository.Add(profile);
        return profile;
    }

    private async Task<FriendRequestDto> BuildFriendRequestDtoAsync(FriendRequest request, Guid perspectiveUserId, CancellationToken cancellationToken)
    {
        var otherUserId = request.RequesterId == perspectiveUserId ? request.AddresseeId : request.RequesterId;
        var other = await _userRepository.GetByIdAsync(otherUserId, cancellationToken)
            ?? await _userRepository.GetByIdAsync(request.RequesterId, cancellationToken)
            ?? throw new InvalidOperationException("Không thể dựng thông tin lời mời kết bạn.");

        return new FriendRequestDto(
            request.Id,
            request.RequesterId,
            request.AddresseeId,
            request.Status,
            new UserSummaryDto(other.Id, other.UserName, other.FullName, other.AvatarUrl),
            request.CreatedDate,
            request.RespondedAtUtc);
    }

    private async Task InvalidateFriendListsAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken)
    {
        foreach (var userId in new[] { userAId, userBId }.Where(x => x != Guid.Empty).Distinct())
        {
            await _redisCache.SetAsync(
                SocialCacheKeys.FriendListVersion(_redisKeyFactory, userId),
                Guid.NewGuid().ToString("N"),
                VersionTtl,
                cancellationToken);
        }
    }

    private async Task PublishFriendEventAsync(Guid userId, string eventName, Guid actorId, object payload, CancellationToken cancellationToken)
    {
        await _socialRealtimePublisher.PublishToUserAsync(
            new SocialRealtimeEnvelope(eventName, userId, actorId, _clock.UtcNow, payload),
            cancellationToken);
    }

    private static int NormalizePage(int pageNumber) => pageNumber <= 0 ? 1 : pageNumber;
    private static int NormalizeSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
}
