using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Notifications;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly DataContext _context;

    public NotificationRepository(DataContext context)
    {
        _context = context;
    }

    public Task<Notification?> GetByIdForUserAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Notifications
            .FirstOrDefaultAsync(
                x => x.Id == notificationId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListByUserAsync(
        Guid userId,
        Guid? workspaceId,
        bool unreadOnly,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (safePageNumber - 1) * safePageSize;

        return await ApplyUserFilter(
                _context.Notifications.AsNoTracking(),
                userId,
                workspaceId,
                unreadOnly)
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByUserAsync(
        Guid userId,
        Guid? workspaceId,
        bool unreadOnly,
        CancellationToken cancellationToken = default)
    {
        return ApplyUserFilter(
                _context.Notifications.AsNoTracking(),
                userId,
                workspaceId,
                unreadOnly)
            .CountAsync(cancellationToken);
    }

    public Task<int> CountUnreadByUserAsync(
        Guid userId,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        return ApplyUserFilter(
                _context.Notifications.AsNoTracking(),
                userId,
                workspaceId,
                unreadOnly: true)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListUnreadByUserAsync(
        Guid userId,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        return await ApplyUserFilter(
                _context.Notifications,
                userId,
                workspaceId,
                unreadOnly: true)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public void Add(Notification notification)
    {
        _context.Notifications.Add(notification);
    }

    public void AddRange(IEnumerable<Notification> notifications)
    {
        _context.Notifications.AddRange(notifications);
    }

    public void Update(Notification notification)
    {
        _context.Notifications.Update(notification);
    }

    private static IQueryable<Notification> ApplyUserFilter(
        IQueryable<Notification> query,
        Guid userId,
        Guid? workspaceId,
        bool unreadOnly)
    {
        query = query.Where(x => x.UserId == userId);

        if (workspaceId.HasValue && workspaceId.Value != Guid.Empty)
        {
            query = query.Where(x => x.WorkspaceId == workspaceId.Value);
        }

        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        return query;
    }
}