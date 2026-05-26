using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Persistence;
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
        Guid? userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
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
            entityType,
            userId,
            fromUtc,
            toUtc);

        query = ApplySearchFilter(query, search);

        // Keep the SQL query simple and stable: fetch the audit rows first, then
        // hydrate user display data in a second query. This avoids EF/Npgsql
        // translation issues caused by left join + DTO projection + ordering.
        var logs = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return [];
        }

        var userIds = logs
            .Select(x => x.UserId)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        var users = await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.FullName,
                x.AvatarUrl
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return logs
            .Select(log =>
            {
                users.TryGetValue(log.UserId, out var user);

                return new ActivityLogDto(
                    log.Id,
                    log.WorkspaceId,
                    log.UserId,
                    user?.UserName,
                    user?.FullName,
                    user?.AvatarUrl,
                    log.Action,
                    log.EntityType,
                    log.EntityId,
                    log.Description,
                    log.MetadataJson,
                    log.IpAddress,
                    log.OccurredAt,
                    log.CreatedDate);
            })
            .ToList();
    }

    public Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        ActivityAction? action,
        ActivityEntityType? entityType,
        Guid? userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(
            _context.ActivityLogs.AsNoTracking(),
            workspaceId,
            action,
            entityType,
            userId,
            fromUtc,
            toUtc);

        return ApplySearchFilter(query, search)
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
        ActivityEntityType? entityType,
        Guid? userId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
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

        if (userId.HasValue)
        {
            query = query.Where(x => x.UserId == userId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAt >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAt <= toUtc.Value);
        }

        return query;
    }

    private IQueryable<ActivityLog> ApplySearchFilter(
        IQueryable<ActivityLog> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var pattern = LikePattern.Contains(search);

        var matchingUserIds = _context.Users
            .AsNoTracking()
            .Where(user =>
                EF.Functions.ILike(user.UserName, pattern) ||
                EF.Functions.ILike(user.FullName, pattern) ||
                EF.Functions.ILike(user.Email, pattern))
            .Select(user => user.Id);

        return query.Where(log =>
            (log.Description != null && EF.Functions.ILike(log.Description, pattern)) ||
            matchingUserIds.Contains(log.UserId));
    }

}
