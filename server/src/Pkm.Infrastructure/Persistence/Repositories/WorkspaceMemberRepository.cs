using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class WorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly DataContext _dataContext;

    public WorkspaceMemberRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<WorkspaceMember?> GetByWorkspaceAndUserAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .FirstOrDefaultAsync(
                x => x.WorkspaceId == workspaceId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceMember>> ListByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.Role)
            .ThenBy(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceMemberReadModel>> ListReadModelsByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId)
            .OrderBy(x => x.Role)
            .ThenBy(x => x.CreatedDate)
            .Select(x => new WorkspaceMemberReadModel(
                x.WorkspaceId,
                x.UserId,
                x.Role,
                x.CreatedDate,
                x.UpdatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .CountAsync(x => x.WorkspaceId == workspaceId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AnyAsync(x => x.WorkspaceId == workspaceId && x.UserId == userId, cancellationToken);
    }

    public void Add(WorkspaceMember member)
    {
        _dataContext.WorkspaceMembers.Add(member);
    }

    public void Update(WorkspaceMember member)
    {
        _dataContext.WorkspaceMembers.Update(member);
    }
}