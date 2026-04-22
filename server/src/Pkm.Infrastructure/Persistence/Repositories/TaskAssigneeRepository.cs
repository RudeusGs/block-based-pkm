using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class TaskAssigneeRepository : ITaskAssigneeRepository
{
    private readonly DataContext _context;

    public TaskAssigneeRepository(DataContext context)
    {
        _context = context;
    }

    public Task<TaskAssignee?> GetByTaskAndUserAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskAssignees
            .FirstOrDefaultAsync(
                x => x.TaskId == taskId && x.UserId == userId,
                cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskAssignees
            .AnyAsync(
                x => x.TaskId == taskId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<TaskAssigneeReadModel>> ListByTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TaskAssignees
            .AsNoTracking()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.CreatedDate)
            .Select(x => new TaskAssigneeReadModel(
                x.TaskId,
                x.UserId,
                x.CreatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> ListUserIdsByTaskIdsAsync(
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<Guid>>();
        }

        var rows = await _context.TaskAssignees
            .AsNoTracking()
            .Where(x => taskIds.Contains(x.TaskId))
            .OrderBy(x => x.CreatedDate)
            .Select(x => new { x.TaskId, x.UserId })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.TaskId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Guid>)x.Select(y => y.UserId).ToArray());
    }

    public void Add(TaskAssignee assignee)
    {
        _context.TaskAssignees.Add(assignee);
    }

    public void Remove(TaskAssignee assignee)
    {
        _context.TaskAssignees.Remove(assignee);
    }
}