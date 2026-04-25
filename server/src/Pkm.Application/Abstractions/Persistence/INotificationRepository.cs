using Pkm.Domain.Notifications;

namespace Pkm.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task<Notification?> GetByIdForUserAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListByUserAsync(
        Guid userId,
        Guid? workspaceId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(
        Guid userId,
        Guid? workspaceId,
        bool unreadOnly,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadByUserAsync(
        Guid userId,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListUnreadByUserAsync(
        Guid userId,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default);

    void Add(Notification notification);

    void AddRange(IEnumerable<Notification> notifications);

    void Update(Notification notification);
}