using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Features.Social.Models;
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
            .AnyAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
    }

    public async Task<Workspace?> GetTrashedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
    }

    public async Task<WorkspaceMember?> GetMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.WorkspaceId == workspaceId &&
                     x.UserId == userId &&
                     !x.IsDeleted,
                cancellationToken);
    }

    public async Task<bool> IsOwnerAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AnyAsync(
                x => x.Id == workspaceId &&
                     x.OwnerId == userId &&
                     !x.IsDeleted,
                cancellationToken);
    }

    public async Task<int> CountMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .CountAsync(x => x.WorkspaceId == workspaceId && !x.IsDeleted, cancellationToken);
    }

    public async Task<WorkspaceAccessReadModel?> GetAccessContextAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AsNoTracking()
            .Where(x => x.Id == workspaceId && !x.IsDeleted)
            .Select(w => new WorkspaceAccessReadModel(
                w.Id,
                w.OwnerId,
                w.OwnerId == userId
                    ? WorkspaceRole.Owner
                    : _dataContext.WorkspaceMembers
                        .AsNoTracking()
                        .Where(m => m.WorkspaceId == w.Id && m.UserId == userId && !m.IsDeleted)
                        .Select(m => (WorkspaceRole?)m.Role)
                        .FirstOrDefault(),
                w.Visibility))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkspaceDetailReadModel?> GetDetailAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .AsNoTracking()
            .Where(x => x.Id == workspaceId && !x.IsDeleted)
            .Select(x => new WorkspaceDetailReadModel(
                x.Id,
                x.Name,
                x.Description,
                x.AvatarUrl,
                x.Visibility,
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
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (safePageNumber - 1) * safePageSize;

        return await _dataContext.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && !m.IsDeleted)
            .Join(
                _dataContext.Workspaces.AsNoTracking().Where(w => !w.IsDeleted),
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
                x.Workspace.AvatarUrl,
                x.Workspace.Visibility,
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
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .Join(
                _dataContext.Workspaces.AsNoTracking().Where(w => !w.IsDeleted),
                member => member.WorkspaceId,
                workspace => workspace.Id,
                (_, _) => 1)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceTrashItemReadModel>> ListTrashedByOwnerAsync(
        Guid ownerUserId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (safePageNumber - 1) * safePageSize;

        return await _dataContext.Workspaces
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(w => w.OwnerId == ownerUserId && w.IsDeleted)
            .OrderByDescending(w => w.DeletedDate ?? w.UpdatedDate ?? w.CreatedDate)
            .Skip(skip)
            .Take(safePageSize)
            .Select(w => new WorkspaceTrashItemReadModel(
                w.Id,
                w.Name,
                w.Description,
                w.AvatarUrl,
                w.Visibility,
                w.OwnerId,
                w.CreatedDate,
                w.UpdatedDate,
                w.DeletedDate,
                WorkspaceRole.Owner))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountTrashedByOwnerAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Workspaces
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(w => w.OwnerId == ownerUserId && w.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<ProfileWorkspaceDto>> ListProfileWorkspacesAsync(
        Guid ownerUserId,
        Guid viewerUserId,
        bool includePrivate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        return await ApplyProfileWorkspaceAccess(ownerUserId, viewerUserId, includePrivate)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProfileWorkspaceDto(
                x.Id,
                x.Name,
                x.Description,
                x.AvatarUrl,
                x.Visibility,
                x.CreatedDate,
                x.UpdatedDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountProfileWorkspacesAsync(
        Guid ownerUserId,
        Guid viewerUserId,
        bool includePrivate,
        CancellationToken cancellationToken = default)
    {
        return await ApplyProfileWorkspaceAccess(ownerUserId, viewerUserId, includePrivate)
            .CountAsync(cancellationToken);
    }

    public void Add(Workspace workspace)
    {
        _dataContext.Workspaces.Add(workspace);
    }

    public void Update(Workspace workspace)
    {
        _dataContext.Workspaces.Update(workspace);
    }

    private IQueryable<Workspace> ApplyProfileWorkspaceAccess(
        Guid ownerUserId,
        Guid viewerUserId,
        bool includePrivate)
    {
        return _dataContext.Workspaces
            .AsNoTracking()
            .Where(x => x.OwnerId == ownerUserId && !x.IsDeleted)
            .Where(x => includePrivate ||
                x.Visibility == WorkspaceVisibility.Public ||
                _dataContext.WorkspaceMembers.Any(m =>
                    m.WorkspaceId == x.Id &&
                    m.UserId == viewerUserId &&
                    !m.IsDeleted));
    }
}
