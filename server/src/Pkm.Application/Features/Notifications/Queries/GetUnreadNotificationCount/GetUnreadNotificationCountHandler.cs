using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(15);

    private readonly ICurrentUser _currentUser;
    private readonly INotificationRepository _notificationRepository;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public GetUnreadNotificationCountHandler(
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
    }

    public async Task<Result<NotificationUnreadCountDto>> HandleAsync(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<NotificationUnreadCountDto>(
                NotificationErrors.InvalidListRequest(new[] { "WorkspaceId không hợp lệ." }));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<NotificationUnreadCountDto>(
                NotificationErrors.MissingUserContext);
        }

        var versionKey = NotificationCacheKeys.UnreadCountVersion(
            _redisKeyFactory,
            currentUserId);

        var version = await _redisCache.GetAsync<string>(
            versionKey,
            cancellationToken) ?? "1";

        var cacheKey = NotificationCacheKeys.UnreadCount(
            _redisKeyFactory,
            currentUserId,
            request.WorkspaceId,
            version);

        var cached = await _redisCache.GetAsync<NotificationUnreadCountDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return Result.Success(cached);

        var count = await _notificationRepository.CountUnreadByUserAsync(
            currentUserId,
            request.WorkspaceId,
            cancellationToken);

        var dto = new NotificationUnreadCountDto(
            currentUserId,
            request.WorkspaceId,
            count);

        await _redisCache.SetAsync(
            cacheKey,
            dto,
            CacheTtl,
            cancellationToken);

        return Result.Success(dto);
    }
}