using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Application.Features.Notifications.Queries.ListNotifications;

public sealed class ListNotificationsHandler
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly ICurrentUser _currentUser;
    private readonly INotificationRepository _notificationRepository;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly ListNotificationsQueryValidator _validator;

    public ListNotificationsHandler(
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        ListNotificationsQueryValidator validator)
    {
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _validator = validator;
    }

    public async Task<Result<NotificationPagedResultDto>> HandleAsync(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<NotificationPagedResultDto>(
                NotificationErrors.InvalidListRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<NotificationPagedResultDto>(
                NotificationErrors.MissingUserContext);
        }

        var versionKey = NotificationCacheKeys.ListVersion(
            _redisKeyFactory,
            currentUserId);

        var version = await _redisCache.GetAsync<string>(
            versionKey,
            cancellationToken) ?? "1";

        var cacheKey = NotificationCacheKeys.List(
            _redisKeyFactory,
            currentUserId,
            request.WorkspaceId,
            request.UnreadOnly,
            request.PageNumber,
            request.PageSize,
            version);

        var cached = await _redisCache.GetAsync<NotificationPagedResultDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return Result.Success(cached);

        var items = await _notificationRepository.ListByUserAsync(
            currentUserId,
            request.WorkspaceId,
            request.UnreadOnly,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _notificationRepository.CountByUserAsync(
            currentUserId,
            request.WorkspaceId,
            request.UnreadOnly,
            cancellationToken);

        var dto = new NotificationPagedResultDto(
            items.Select(x => x.ToDto()).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize));

        await _redisCache.SetAsync(
            cacheKey,
            dto,
            CacheTtl,
            cancellationToken);

        return Result.Success(dto);
    }
}