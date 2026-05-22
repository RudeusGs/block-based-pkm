using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Activity.Models;
using Pkm.Domain.Audit;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class ActivityLogRepository : IActivityLogRepository
{
    private readonly DataContext _context;

    public ActivityLogRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ActivityLogDto>> ListByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 30 : Math.Min(pageSize, 100);
        var skip = (safePageNumber - 1) * safePageSize;

        var query = ApplyFilter(
            _context.ActivityLogs.AsNoTracking(),
            workspaceId,
            action,
            entityType);

        return await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Join(
                _context.Users.AsNoTracking(),
                log => log.UserId,
                user => user.Id,
                (log, user) => new ActivityLogDto(
                    log.Id,
                    log.WorkspaceId,
                    log.UserId,
                    user.UserName,
                    user.FullName,
                    user.AvatarUrl,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    log.Description,
                    log.MetadataJson,
                    log.IpAddress,
                    log.OccurredAt,
                    log.CreatedDate))
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilter(
                _context.ActivityLogs.AsNoTracking(),
                workspaceId,
                action,
                entityType)
            .CountAsync(cancellationToken);
    }

    public void Add(ActivityLog activityLog)
    {
        _context.ActivityLogs.Add(activityLog);
    }

    private static IQueryable<ActivityLog> ApplyFilter(
        IQueryable<ActivityLog> query,
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType)
    {
        query = query.Where(x => x.WorkspaceId == workspaceId);

        if (action.HasValue)
        {
            query = query.Where(x => x.Action == action.Value);
        }

        if (entityType.HasValue)
        {
            query = query.Where(x => x.EntityType == entityType.Value);
        }

        return query;
    }
}
