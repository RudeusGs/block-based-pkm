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
        return _context.Set<WorkTask>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TaskAccessReadModel?> GetAccessContextAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var raw = await (
            from task in _context.Set<WorkTask>().AsNoTracking()
            join member in _context.Set<WorkspaceMember>().AsNoTracking().Where(x => x.UserId == userId)
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

        var role = raw.Role;

        if (raw.CreatedById == userId && role is null)
        {
            role = WorkspaceRole.Owner;
        }

        return new TaskAccessReadModel(
            raw.Id,
            raw.WorkspaceId,
            raw.CreatedById,
            role,
            raw.Status);
    }
}