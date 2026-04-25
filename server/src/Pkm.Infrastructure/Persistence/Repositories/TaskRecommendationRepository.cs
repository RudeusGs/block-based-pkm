using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Recommendations;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class TaskRecommendationRepository : ITaskRecommendationRepository
{
    private readonly DataContext _context;

    public TaskRecommendationRepository(DataContext context)
    {
        _context = context;
    }

    public Task<TaskRecommendation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskRecommendations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TaskRecommendation?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskRecommendations
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskRecommendation>> ListByUserAsync(
        Guid userId,
        Guid? workspaceId,
        StatusTaskRecommendation? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (safePageNumber - 1) * safePageSize;

        return await ApplyFilter(
                _context.TaskRecommendations.AsNoTracking(),
                userId,
                workspaceId,
                status)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByUserAsync(
        Guid userId,
        Guid? workspaceId,
        StatusTaskRecommendation? status,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilter(
                _context.TaskRecommendations.AsNoTracking(),
                userId,
                workspaceId,
                status)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskRecommendation>> ListPendingByUserAndWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TaskRecommendations
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.WorkspaceId == workspaceId &&
                x.Status == StatusTaskRecommendation.Pending)
            .OrderByDescending(x => x.Score)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasPendingForTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskRecommendations.AnyAsync(
            x =>
                x.UserId == userId &&
                x.TaskId == taskId &&
                x.Status == StatusTaskRecommendation.Pending,
            cancellationToken);
    }

    public void Add(TaskRecommendation recommendation)
    {
        _context.TaskRecommendations.Add(recommendation);
    }

    public void AddRange(IEnumerable<TaskRecommendation> recommendations)
    {
        _context.TaskRecommendations.AddRange(recommendations);
    }

    public void Update(TaskRecommendation recommendation)
    {
        _context.TaskRecommendations.Update(recommendation);
    }

    private static IQueryable<TaskRecommendation> ApplyFilter(
        IQueryable<TaskRecommendation> query,
        Guid userId,
        Guid? workspaceId,
        StatusTaskRecommendation? status)
    {
        query = query.Where(x => x.UserId == userId);

        if (workspaceId.HasValue && workspaceId.Value != Guid.Empty)
            query = query.Where(x => x.WorkspaceId == workspaceId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        return query;
    }
}