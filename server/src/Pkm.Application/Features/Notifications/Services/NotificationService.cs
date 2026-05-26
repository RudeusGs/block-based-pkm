using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Features.Notifications.Models;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Notifications;

namespace Pkm.Application.Features.Notifications.Services;

public sealed class NotificationService : INotificationService
{
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    private readonly INotificationRepository _notificationRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationRealtimePublisher _notificationRealtimePublisher;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;

    public NotificationService(
        INotificationRepository notificationRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        INotificationRealtimePublisher notificationRealtimePublisher,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock)
    {
        _notificationRepository = notificationRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _notificationRealtimePublisher = notificationRealtimePublisher;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
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

        var rawRecipients = recipientUserIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var recipients = rawRecipients
            .Where(x => !excluded.Contains(x))
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
        }

        await PublishUnreadCountChangedAsync(
            dtos.Select(x => x.UserId).ToArray(),
            request.WorkspaceId,
            request.ActorUserId,
            cancellationToken);

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

        var members = await _workspaceMemberRepository.ListByWorkspaceAsync(
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
            _cacheKeyFactory,
            userId);

        var unreadVersionKey = NotificationCacheKeys.UnreadCountVersion(
            _cacheKeyFactory,
            userId);

        var newVersion = Guid.NewGuid().ToString("N");

        await _cache.SetAsync(
            listVersionKey,
            newVersion,
            VersionTtl,
            cancellationToken);

        await _cache.SetAsync(
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
        await PublishUnreadCountChangedAsync(
            new[] { userId },
            workspaceId,
            actorUserId,
            cancellationToken);
    }

    private async Task PublishUnreadCountChangedAsync(
        IReadOnlyCollection<Guid> userIds,
        Guid? workspaceId,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserIds = userIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedUserIds.Length == 0)
            return;

        var totalUnreadByUser = await _notificationRepository.CountUnreadByUsersAsync(
            normalizedUserIds,
            workspaceId: null,
            cancellationToken);

        var workspaceUnreadByUser = workspaceId.HasValue && workspaceId.Value != Guid.Empty
            ? await _notificationRepository.CountUnreadByUsersAsync(
                normalizedUserIds,
                workspaceId,
                cancellationToken)
            : totalUnreadByUser;

        foreach (var userId in normalizedUserIds)
        {
            totalUnreadByUser.TryGetValue(userId, out var totalUnread);
            workspaceUnreadByUser.TryGetValue(userId, out var workspaceUnread);

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

}
