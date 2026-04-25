using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Recommendations;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class UserTaskHistoryRepository : IUserTaskHistoryRepository
{
    private readonly DataContext _context;

    public UserTaskHistoryRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UserTaskHistory>> ListRecentByUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        take = take <= 0 ? 50 : Math.Min(take, 500);

        return await _context.UserTaskHistories
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedDate)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserTaskHistoryStatsDto> GetStatsByUserAndWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.UserTaskHistories
            .AsNoTracking()
            .Join(
                _context.WorkTasks.AsNoTracking(),
                history => history.TaskId,
                task => task.Id,
                (history, task) => new
                {
                    History = history,
                    Task = task
                })
            .Where(x =>
                x.History.UserId == userId &&
                x.Task.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        var completed = rows
            .Where(x => x.History.Status == StatusUserTaskHistory.Completed)
            .ToArray();

        var skippedOrAbandoned = rows
            .Where(x =>
                x.History.Status == StatusUserTaskHistory.Skipped ||
                x.History.Status == StatusUserTaskHistory.Abandoned)
            .ToArray();

        var completedByTaskId = completed
            .GroupBy(x => x.History.TaskId)
            .ToDictionary(x => x.Key, x => x.Count());

        var skippedOrAbandonedByTaskId = skippedOrAbandoned
            .GroupBy(x => x.History.TaskId)
            .ToDictionary(x => x.Key, x => x.Count());

        var completedCount = completed.Length;
        var abandonedCount = rows.Count(x => x.History.Status == StatusUserTaskHistory.Abandoned);
        var skippedCount = rows.Count(x => x.History.Status == StatusUserTaskHistory.Skipped);

        var avgDuration = completed.Length == 0
            ? 0
            : completed.Average(x => x.History.DurationMinutes);

        return new UserTaskHistoryStatsDto(
            userId,
            workspaceId,
            completedCount,
            abandonedCount,
            skippedCount,
            avgDuration,
            completedByTaskId,
            skippedOrAbandonedByTaskId);
    }

    public void Add(UserTaskHistory history)
    {
        _context.UserTaskHistories.Add(history);
    }

    public void Update(UserTaskHistory history)
    {
        _context.UserTaskHistories.Update(history);
    }
}