using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class WorkTaskRepository : IWorkTaskRepository
{
    private readonly DataContext _context;

    public WorkTaskRepository(DataContext context)
    {
        _context = context;
    }

    public Task<WorkTask?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.WorkTasks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<WorkTask?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.WorkTasks
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.WorkTasks.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<WorkTaskDetailReadModel?> GetDetailAsync(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _context.WorkTasks
            .AsNoTracking()
            .Where(x => x.Id == taskId)
            .Select(x => new
            {
                x.Id,
                x.WorkspaceId,
                x.PageId,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.CreatedById,
                x.LastModifiedById,
                x.CreatedDate,
                x.UpdatedDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (task is null)
            return null;

        var assignees = await _context.TaskAssignees
            .AsNoTracking()
            .Where(x => x.TaskId == taskId)
            .OrderBy(x => x.CreatedDate)
            .Select(x => new TaskAssigneeReadModel(
                x.TaskId,
                x.UserId,
                x.CreatedDate))
            .ToListAsync(cancellationToken);

        return new WorkTaskDetailReadModel(
            task.Id,
            task.WorkspaceId,
            task.PageId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            task.CreatedById,
            task.LastModifiedById,
            task.CreatedDate,
            task.UpdatedDate,
            assignees);
    }

    public async Task<IReadOnlyList<WorkTaskListItemReadModel>> ListByWorkspaceAsync(
        Guid workspaceId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

        var query = ApplyFilter(
            _context.WorkTasks
                .AsNoTracking()
                .Where(x => x.WorkspaceId == workspaceId),
            filter);

        return await query
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new WorkTaskListItemReadModel(
                x.Id,
                x.WorkspaceId,
                x.PageId,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.CreatedById,
                x.LastModifiedById,
                x.CreatedDate,
                x.UpdatedDate,
                _context.TaskAssignees.Count(a => a.TaskId == x.Id)))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(
            _context.WorkTasks
                .AsNoTracking()
                .Where(x => x.WorkspaceId == workspaceId),
            filter);

        return query.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkTaskListItemReadModel>> ListByPageAsync(
        Guid pageId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = filter.PageNumber <= 0 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize <= 0 ? 20 : Math.Min(filter.PageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

        var query = ApplyFilter(
            _context.WorkTasks
                .AsNoTracking()
                .Where(x => x.PageId == pageId),
            filter);

        return await query
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate)
            .ThenByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new WorkTaskListItemReadModel(
                x.Id,
                x.WorkspaceId,
                x.PageId,
                x.Title,
                x.Description,
                x.Status,
                x.Priority,
                x.DueDate,
                x.CreatedById,
                x.LastModifiedById,
                x.CreatedDate,
                x.UpdatedDate,
                _context.TaskAssignees.Count(a => a.TaskId == x.Id)))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountByPageAsync(
        Guid pageId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(
            _context.WorkTasks
                .AsNoTracking()
                .Where(x => x.PageId == pageId),
            filter);

        return query.CountAsync(cancellationToken);
    }

    public async Task<TaskAccessReadModel?> GetAccessContextAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var raw = await (
            from task in _context.WorkTasks.AsNoTracking()
            join member in _context.WorkspaceMembers.AsNoTracking().Where(x => x.UserId == userId)
                on task.WorkspaceId equals member.WorkspaceId into memberGroup
            from member in memberGroup.DefaultIfEmpty()
            where task.Id == taskId
            select new
            {
                task.Id,
                task.WorkspaceId,
                task.CreatedById,
                task.Status,
                Role = member != null ? (WorkspaceRole?)member.Role : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (raw is null)
            return null;

        return new TaskAccessReadModel(
            raw.Id,
            raw.WorkspaceId,
            raw.CreatedById,
            raw.Role,
            raw.Status);
    }

    public void Add(WorkTask task)
    {
        _context.WorkTasks.Add(task);
    }

    public void Update(WorkTask task)
    {
        _context.WorkTasks.Update(task);
    }

    private IQueryable<WorkTask> ApplyFilter(
        IQueryable<WorkTask> query,
        WorkTaskListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }
        else if (!filter.IncludeCompleted)
        {
            query = query.Where(x => x.Status != StatusWorkTask.Done);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == filter.Priority.Value);
        }

        if (filter.AssigneeUserId.HasValue && filter.AssigneeUserId.Value != Guid.Empty)
        {
            var assigneeUserId = filter.AssigneeUserId.Value;
            query = query.Where(x =>
                _context.TaskAssignees.Any(a =>
                    a.TaskId == x.Id &&
                    a.UserId == assigneeUserId));
        }

        if (filter.DueFrom.HasValue)
        {
            query = query.Where(x =>
                x.DueDate.HasValue &&
                x.DueDate.Value >= filter.DueFrom.Value);
        }

        if (filter.DueTo.HasValue)
        {
            query = query.Where(x =>
                x.DueDate.HasValue &&
                x.DueDate.Value <= filter.DueTo.Value);
        }

        return query;
    }
}
