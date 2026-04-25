using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Tasks;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class TaskCommentRepository : ITaskCommentRepository
{
    private readonly DataContext _context;

    public TaskCommentRepository(DataContext context)
    {
        _context = context;
    }

    public Task<TaskComment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskComments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TaskComment?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskComments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TaskComment?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskComments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<TaskComment?> GetByIdForUpdateIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TaskComments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskComment>> ListByTaskAsync(
        Guid taskId,
        int pageNumber,
        int pageSize,
        bool includeDeleted = true,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 50 : Math.Min(pageSize, 100);
        var skip = (safePageNumber - 1) * safePageSize;

        var query = includeDeleted
            ? _context.TaskComments.IgnoreQueryFilters().AsNoTracking()
            : _context.TaskComments.AsNoTracking();

        return await query
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.CreatedDate)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByTaskAsync(
        Guid taskId,
        bool includeDeleted = true,
        CancellationToken cancellationToken = default)
    {
        var query = includeDeleted
            ? _context.TaskComments.IgnoreQueryFilters().AsNoTracking()
            : _context.TaskComments.AsNoTracking();

        return query.CountAsync(x => x.TaskId == taskId, cancellationToken);
    }

    public Task<bool> ExistsInTaskAsync(
        Guid commentId,
        Guid taskId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = includeDeleted
            ? _context.TaskComments.IgnoreQueryFilters().AsNoTracking()
            : _context.TaskComments.AsNoTracking();

        return query.AnyAsync(
            x => x.Id == commentId && x.TaskId == taskId,
            cancellationToken);
    }

    public void Add(TaskComment comment)
    {
        _context.TaskComments.Add(comment);
    }

    public void Update(TaskComment comment)
    {
        _context.TaskComments.Update(comment);
    }
}