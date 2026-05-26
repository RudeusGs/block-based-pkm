using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Features.Pages.Models;
using Pkm.Domain.Pages;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class PageRepository : IPageReadRepository, IPageWriteRepository
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

    public async Task<Page?> GetByIdIncludingArchivedForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Pages
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dataContext.Pages
            .AnyAsync(x => x.Id == id && !x.IsArchived, cancellationToken);

    public async Task<bool> ExistsByPublicTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicToken))
            return false;

        return await
            (from page in _dataContext.Pages.AsNoTracking()
             join workspace in _dataContext.Workspaces.AsNoTracking()
                 on page.WorkspaceId equals workspace.Id
             where page.IsPublished
                   && page.PublicToken == publicToken
                   && !page.IsArchived
             select page.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<Page?> GetPublishedByTokenAsync(
        string publicToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicToken))
            return null;

        return await
            (from page in _dataContext.Pages.AsNoTracking()
             join workspace in _dataContext.Workspaces.AsNoTracking()
                 on page.WorkspaceId equals workspace.Id
             where page.IsPublished
                   && page.PublicToken == publicToken
                   && !page.IsArchived
             select page)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Page>> ListByWorkspaceAsync(
        Guid workspaceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && !x.IsArchived)
            .OrderBy(x => x.ParentPageId)
            .ThenByDescending(x => x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Page>> ListRecentlyUpdatedByWorkspaceAsync(
        Guid workspaceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && !x.IsArchived)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
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
        return await ApplySubPagesFilter(parentPageId)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Page>> ListSubPagesAsync(
        Guid parentPageId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        return await ApplySubPagesFilter(parentPageId)
            .OrderByDescending(x => x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountSubPagesAsync(
        Guid parentPageId,
        CancellationToken cancellationToken = default)
    {
        return await ApplySubPagesFilter(parentPageId)
            .CountAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<Page>> ListArchivedByWorkspaceAsync(
        Guid workspaceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && x.IsArchived)
            .OrderByDescending(x => x.ArchivedAt ?? x.UpdatedDate ?? x.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountArchivedByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dataContext.Pages
            .CountAsync(x => x.WorkspaceId == workspaceId && x.IsArchived, cancellationToken);
    }

    public async Task<IReadOnlyList<Page>> SearchInWorkspaceAsync(
        Guid workspaceId,
        string keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var pattern = LikePattern.Contains(keyword ?? string.Empty);

        return await _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && !x.IsArchived)
            .Where(x => EF.Functions.ILike(x.Title, pattern))
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
        var pattern = LikePattern.Contains(keyword ?? string.Empty);

        return await _dataContext.Pages
            .CountAsync(
                x => x.WorkspaceId == workspaceId
                     && !x.IsArchived
                     && EF.Functions.ILike(x.Title, pattern),
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
                 workspace.Visibility,
                 page.IsArchived))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SoftDeleteExpiredArchivedPagesAsync(
        DateTimeOffset archiveCutoffUtc,
        DateTimeOffset deletedAtUtc,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        batchSize = batchSize <= 0 ? 100 : Math.Min(batchSize, 500);

        var rootPageIds = await _dataContext.Pages
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted
                        && x.IsArchived
                        && x.ArchivedAt.HasValue
                        && x.ArchivedAt.Value <= archiveCutoffUtc)
            .OrderBy(x => x.ArchivedAt)
            .Select(x => x.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (rootPageIds.Count == 0)
            return 0;

        var pageIdsToDelete = await CollectPageTreeIdsAsync(rootPageIds, cancellationToken);

        var pages = await _dataContext.Pages
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && pageIdsToDelete.Contains(x.Id))
            .ToListAsync(cancellationToken);

        foreach (var page in pages)
            page.SoftDelete(deletedAtUtc);

        var blocks = await _dataContext.Blocks
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && pageIdsToDelete.Contains(x.PageId))
            .ToListAsync(cancellationToken);

        foreach (var block in blocks)
            block.SoftDelete(deletedAtUtc);

        var favorites = await _dataContext.PageFavorites
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && pageIdsToDelete.Contains(x.PageId))
            .ToListAsync(cancellationToken);

        foreach (var favorite in favorites)
            favorite.SoftDelete(deletedAtUtc);

        var recents = await _dataContext.PageRecents
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && pageIdsToDelete.Contains(x.PageId))
            .ToListAsync(cancellationToken);

        foreach (var recent in recents)
            recent.SoftDelete(deletedAtUtc);

        var tasks = await _dataContext.WorkTasks
            .IgnoreQueryFilters()
            .Where(x => !x.IsDeleted
                        && x.PageId.HasValue
                        && pageIdsToDelete.Contains(x.PageId.Value))
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
            task.SoftDelete(deletedAtUtc);

        return pages.Count;
    }

    private async Task<HashSet<Guid>> CollectPageTreeIdsAsync(
        IReadOnlyCollection<Guid> rootPageIds,
        CancellationToken cancellationToken)
    {
        var pageIds = rootPageIds.ToHashSet();
        var frontier = rootPageIds.ToArray();

        while (frontier.Length > 0)
        {
            var childIds = await _dataContext.Pages
                .IgnoreQueryFilters()
                .Where(x => !x.IsDeleted
                            && x.ParentPageId.HasValue
                            && frontier.Contains(x.ParentPageId.Value))
                .Select(x => x.Id)
                .ToArrayAsync(cancellationToken);

            frontier = childIds
                .Where(pageIds.Add)
                .ToArray();
        }

        return pageIds;
    }

    private IQueryable<Page> ApplySubPagesFilter(Guid parentPageId)
    {
        return _dataContext.Pages
            .AsNoTracking()
            .Where(x => x.ParentPageId == parentPageId && !x.IsArchived);
    }

    public void Add(Page page) => _dataContext.Pages.Add(page);
}



