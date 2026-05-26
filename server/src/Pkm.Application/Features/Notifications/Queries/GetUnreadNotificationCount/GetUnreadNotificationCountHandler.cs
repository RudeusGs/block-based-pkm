using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Notifications.Models;

namespace Pkm.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountHandler : IQueryHandler<GetUnreadNotificationCountQuery, NotificationUnreadCountDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(15);

    private readonly ICurrentUser _currentUser;
    private readonly INotificationRepository _notificationRepository;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;

    public GetUnreadNotificationCountHandler(
        ICurrentUser currentUser,
        INotificationRepository notificationRepository,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory)
    {
        _currentUser = currentUser;
        _notificationRepository = notificationRepository;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
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
            _cacheKeyFactory,
            currentUserId);

        var version = await _cache.GetAsync<string>(
            versionKey,
            cancellationToken) ?? "1";

        var cacheKey = NotificationCacheKeys.UnreadCount(
            _cacheKeyFactory,
            currentUserId,
            request.WorkspaceId,
            version);

        var cached = await _cache.GetAsync<NotificationUnreadCountDto>(
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

        await _cache.SetAsync(
            cacheKey,
            dto,
            CacheTtl,
            cancellationToken);

        return Result.Success(dto);
    }
}
