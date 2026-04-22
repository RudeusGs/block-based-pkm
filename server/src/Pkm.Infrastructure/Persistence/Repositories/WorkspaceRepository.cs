using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceRepository : IWorkspaceRepository
{
    private readonly DataContext _dataContext;

    public WorkspaceRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<WorkspaceMember?> GetMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.WorkspaceId == workspaceId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<bool> IsOwnerAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AnyAsync(x => x.Id == workspaceId && x.OwnerId == userId, cancellationToken);
    }

    public async Task<WorkspaceAccessReadModel?> GetAccessContextAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AsNoTracking()
            .Where(x => x.Id == workspaceId)
            .Select(w => new WorkspaceAccessReadModel(
                w.Id,
                w.OwnerId,
                w.OwnerId == userId
                    ? WorkspaceRole.Owner
                    : _dataContext.WorkspaceMembers
                        .AsNoTracking()
                        .Where(m => m.WorkspaceId == w.Id && m.UserId == userId)
                        .Select(m => (WorkspaceRole?)m.Role)
                        .FirstOrDefault()))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkspaceDetailReadModel?> GetDetailAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AsNoTracking()
            .Where(x => x.Id == workspaceId)
            .Select(x => new WorkspaceDetailReadModel(
                x.Id,
                x.Name,
                x.Description,
                x.OwnerId,
                x.LastModifiedBy,
                x.CreatedDate,
                x.UpdatedDate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceListItemReadModel>> ListByUserAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 20 : pageSize;
        var skip = (safePageNumber - 1) * safePageSize;

        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(
                _dataContext.Workspaces.AsNoTracking(),
                member => member.WorkspaceId,
                workspace => workspace.Id,
                (member, workspace) => new
                {
                    Member = member,
                    Workspace = workspace
                })
            .OrderByDescending(x => x.Workspace.UpdatedDate ?? x.Workspace.CreatedDate)
            .Skip(skip)
            .Take(safePageSize)
            .Select(x => new WorkspaceListItemReadModel(
                x.Workspace.Id,
                x.Workspace.Name,
                x.Workspace.Description,
                x.Workspace.OwnerId,
                x.Workspace.CreatedDate,
                x.Workspace.UpdatedDate,
                x.Member.Role))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId, cancellationToken);
    }

    public void Add(Workspace workspace)
    {
        _dataContext.Workspaces.Add(workspace);
    }

    public void Update(Workspace workspace)
    {
        _dataContext.Workspaces.Update(workspace);
    }
}