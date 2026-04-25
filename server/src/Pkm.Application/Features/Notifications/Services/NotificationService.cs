using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Features.Notifications.Models;
using Pkm.Domain.Common;
using Pkm.Domain.Notifications;

namespace Pkm.Application.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    private readonly INotificationRepository _notificationRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationRealtimePublisher _notificationRealtimePublisher;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IClock _clock;

    public NotificationService(
        INotificationRepository notificationRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        INotificationRealtimePublisher notificationRealtimePublisher,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IClock clock)
    {
        _notificationRepository = notificationRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _notificationRealtimePublisher = notificationRealtimePublisher;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _clock = clock;
    }

    public async Task<NotificationDto?> NotifyAsync(
        Guid recipientUserId,
        NotificationDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await NotifyManyAsync(
            new[] { recipientUserId },
            request,
            excludeUserIds: null,
            cancellationToken);

        return result.FirstOrDefault();
    }

    public async Task<IReadOnlyList<NotificationDto>> NotifyManyAsync(
        IEnumerable<Guid> recipientUserIds,
        NotificationDispatchRequest request,
        IEnumerable<Guid>? excludeUserIds = null,
        CancellationToken cancellationToken = default)
    {
        var excluded = (excludeUserIds ?? Array.Empty<Guid>())
            .Where(x => x != Guid.Empty)
            .ToHashSet();

        var recipients = recipientUserIds
            .Where(x => x != Guid.Empty)
            .Where(x => !excluded.Contains(x))
            .Distinct()
            .ToArray();

        if (recipients.Length == 0)
            return Array.Empty<NotificationDto>();

        var now = _clock.UtcNow;

        var notifications = new List<Notification>();

        foreach (var recipientUserId in recipients)
        {
            try
            {
                notifications.Add(new Notification(
                    Guid.NewGuid(),
                    recipientUserId,
                    request.WorkspaceId,
                    request.Type,
                    request.Title,
                    request.Message,
                    now,
                    request.ReferenceId,
                    request.ReferenceType));
            }
            catch (DomainException)
            {
                // Skip invalid notification payload instead of breaking core workflow.
                // Validation still happens in domain constructor.
            }
        }

        if (notifications.Count == 0)
            return Array.Empty<NotificationDto>();

        _notificationRepository.AddRange(notifications);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dtos = notifications
            .Select(x => x.ToDto())
            .ToArray();

        foreach (var dto in dtos)
        {
            await InvalidateUserAsync(dto.UserId, cancellationToken);

            await _notificationRealtimePublisher.PublishToUserAsync(
                new NotificationRealtimeEnvelope(
                    EventName: "NotificationCreated",
                    UserId: dto.UserId,
                    WorkspaceId: dto.WorkspaceId,
                    ActorId: request.ActorUserId,
                    OccurredAtUtc: now,
                    Payload: dto),
                cancellationToken);

            await PublishUnreadCountChangedAsync(
                dto.UserId,
                dto.WorkspaceId,
                request.ActorUserId,
                cancellationToken);
        }

        return dtos;
    }

    public async Task<IReadOnlyList<NotificationDto>> NotifyWorkspaceAsync(
        Guid workspaceId,
        NotificationDispatchRequest request,
        IEnumerable<Guid>? excludeUserIds = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return Array.Empty<NotificationDto>();

        var members = await _workspaceMemberRepository.ListReadModelsByWorkspaceAsync(
            workspaceId,
            cancellationToken);

        var recipients = members
            .Select(x => x.UserId)
            .Distinct()
            .ToArray();

        var requestWithWorkspace = request with
        {
            WorkspaceId = workspaceId
        };

        return await NotifyManyAsync(
            recipients,
            requestWithWorkspace,
            excludeUserIds,
            cancellationToken);
    }

    public async Task InvalidateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return;

        var listVersionKey = NotificationCacheKeys.ListVersion(
            _redisKeyFactory,
            userId);

        var unreadVersionKey = NotificationCacheKeys.UnreadCountVersion(
            _redisKeyFactory,
            userId);

        var newVersion = Guid.NewGuid().ToString("N");

        await _redisCache.SetAsync(
            listVersionKey,
            newVersion,
            VersionTtl,
            cancellationToken);

        await _redisCache.SetAsync(
            unreadVersionKey,
            newVersion,
            VersionTtl,
            cancellationToken);
    }

    public async Task PublishUnreadCountChangedAsync(
        Guid userId,
        Guid? workspaceId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return;

        var totalUnread = await _notificationRepository.CountUnreadByUserAsync(
            userId,
            workspaceId: null,
            cancellationToken);

        var workspaceUnread = workspaceId.HasValue && workspaceId.Value != Guid.Empty
            ? await _notificationRepository.CountUnreadByUserAsync(
                userId,
                workspaceId,
                cancellationToken)
            : totalUnread;

        await _notificationRealtimePublisher.PublishToUserAsync(
            new NotificationRealtimeEnvelope(
                EventName: "NotificationUnreadCountChanged",
                UserId: userId,
                WorkspaceId: workspaceId,
                ActorId: actorUserId,
                OccurredAtUtc: _clock.UtcNow,
                Payload: new
                {
                    userId,
                    workspaceId,
                    unreadCount = workspaceUnread,
                    totalUnreadCount = totalUnread
                }),
            cancellationToken);
    }
}