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

        return await BuildReadModelQuery(query, search)
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
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

        return BuildReadModelQuery(query, search)
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

    private IQueryable<ActivityLogDto> BuildReadModelQuery(
        IQueryable<ActivityLog> query,
        string? search)
    {
        var joined = query
            .GroupJoin(
                _context.Users.AsNoTracking(),
                log => log.UserId,
                user => user.Id,
                (log, users) => new
                {
                    Log = log,
                    Users = users
                })
            .SelectMany(
                x => x.Users.DefaultIfEmpty(),
                (x, user) => new
                {
                    x.Log,
                    User = user
                });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{EscapeLikePattern(search.Trim())}%";

            joined = joined.Where(x =>
                (x.Log.Description != null && EF.Functions.ILike(x.Log.Description, pattern)) ||
                (x.User != null && EF.Functions.ILike(x.User.UserName, pattern)) ||
                (x.User != null && EF.Functions.ILike(x.User.FullName, pattern)) ||
                (x.User != null && EF.Functions.ILike(x.User.Email, pattern)));
        }

        return joined.Select(x => new ActivityLogDto(
            x.Log.Id,
            x.Log.WorkspaceId,
            x.Log.UserId,
            x.User == null ? null : x.User.UserName,
            x.User == null ? null : x.User.FullName,
            x.User == null ? null : x.User.AvatarUrl,
            x.Log.Action,
            x.Log.EntityType,
            x.Log.EntityId,
            x.Log.Description,
            x.Log.MetadataJson,
            x.Log.IpAddress,
            x.Log.OccurredAt,
            x.Log.CreatedDate));
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
    }
}
