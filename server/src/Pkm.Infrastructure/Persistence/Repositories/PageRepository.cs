using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Pages.Models;
using Pkm.Domain.Pages;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class PageRepository : IPageRepository
{
    private readonly DataContext _dataContext;

    public PageRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<Page?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);

    public async Task<Page?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Pages
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsArchived, cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Pages
            .AnyAsync(x => x.Id == id && !x.IsArchived, cancellationToken);

    public async Task<IReadOnlyList<Page>> ListByWorkspaceAsync(
        Guid workspaceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && !x.IsArchived)
            .OrderBy(x => x.ParentPageId)
            .ThenByDescending(x => x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Pages
            .CountAsync(x => x.WorkspaceId == workspaceId && !x.IsArchived, cancellationToken);
    }

    public async Task<IReadOnlyList<Page>> ListSubPagesAsync(
        Guid parentPageId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.ParentPageId == parentPageId && !x.IsArchived)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Page>> SearchInWorkspaceAsync(
        Guid workspaceId,
        string keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : pageSize;
        keyword = (keyword ?? string.Empty).Trim();

        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && !x.IsArchived)
            .Where(x => EF.Functions.ILike(x.Title, $"%{keyword}%"))
            .OrderByDescending(x => x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSearchInWorkspaceAsync(
        Guid workspaceId,
        string keyword,
        CancellationToken cancellationToken = default)
    {
        keyword = (keyword ?? string.Empty).Trim();

        return await _dataContext.Pages
            .CountAsync(
                x => x.WorkspaceId == workspaceId
                     && !x.IsArchived
                     && EF.Functions.ILike(x.Title, $"%{keyword}%"),
                cancellationToken);
    }

    public async Task<PageAccessContextReadModel?> GetAccessContextAsync(
        Guid pageId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (pageId == Guid.Empty || userId == Guid.Empty)
            return null;

        return await
            (from page in _dataContext.Pages.AsNoTracking()
             where page.Id == pageId
             join workspace in _dataContext.Workspaces.AsNoTracking()
                 on page.WorkspaceId equals workspace.Id
             join member in _dataContext.WorkspaceMembers.AsNoTracking()
                    .Where(x => x.UserId == userId && !x.IsDeleted)
                 on workspace.Id equals member.WorkspaceId into memberships
             from membership in memberships.DefaultIfEmpty()
             select new PageAccessContextReadModel(
                 page.Id,
                 page.WorkspaceId,
                 workspace.OwnerId,
                 membership != null ? membership.Role : null,
                 page.IsArchived))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Page page) => _dataContext.Pages.Add(page);
}