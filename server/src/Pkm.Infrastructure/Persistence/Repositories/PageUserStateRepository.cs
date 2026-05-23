using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Pages.Models;
using Pkm.Domain.Pages;
using Pkm.Domain.Workspaces;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class PageUserStateRepository : IPageUserStateRepository
{
    private readonly DataContext _context;

    public PageUserStateRepository(DataContext context)
    {
        _context = context;
    }

    public Task<PageFavorite?> GetFavoriteAsync(
        Guid userId,
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        return _context.PageFavorites
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PageId == pageId, cancellationToken);
    }

    public Task<PageRecent?> GetRecentAsync(
        Guid userId,
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        return _context.PageRecents
            .FirstOrDefaultAsync(x => x.UserId == userId && x.PageId == pageId, cancellationToken);
    }

    public async Task<IReadOnlyList<PageQuickAccessDto>> ListFavoritesAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = NormalizePage(pageNumber);
        pageSize = NormalizeSize(pageSize);

        return await (
                from favorite in _context.PageFavorites.AsNoTracking()
                where favorite.UserId == userId
                join page in _context.Pages.AsNoTracking().Where(x => !x.IsArchived)
                    on favorite.PageId equals page.Id
                join workspace in _context.Workspaces.AsNoTracking()
                    on page.WorkspaceId equals workspace.Id
                where workspace.OwnerId == userId
                      || workspace.Visibility == WorkspaceVisibility.Public
                      || _context.WorkspaceMembers.Any(member =>
                          member.WorkspaceId == workspace.Id && member.UserId == userId)
                orderby favorite.CreatedDate descending
                select new PageQuickAccessDto(
                    page.Id,
                    page.WorkspaceId,
                    page.ParentPageId,
                    workspace.Name,
                    page.Title,
                    page.Icon,
                    page.CoverImage,
                    page.IsArchived,
                    page.CurrentRevision,
                    page.CreatedDate,
                    page.UpdatedDate,
                    true,
                    favorite.CreatedDate,
                    _context.PageRecents
                        .Where(recent => recent.UserId == userId && recent.PageId == page.Id)
                        .Select(recent => (DateTimeOffset?)recent.LastVisitedAtUtc)
                        .FirstOrDefault(),
                    _context.PageRecents
                        .Where(recent => recent.UserId == userId && recent.PageId == page.Id)
                        .Select(recent => recent.VisitCount)
                        .FirstOrDefault()))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountFavoritesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return (
                from favorite in _context.PageFavorites.AsNoTracking()
                where favorite.UserId == userId
                join page in _context.Pages.AsNoTracking().Where(x => !x.IsArchived)
                    on favorite.PageId equals page.Id
                join workspace in _context.Workspaces.AsNoTracking()
                    on page.WorkspaceId equals workspace.Id
                where workspace.OwnerId == userId
                      || workspace.Visibility == WorkspaceVisibility.Public
                      || _context.WorkspaceMembers.Any(member =>
                          member.WorkspaceId == workspace.Id && member.UserId == userId)
                select favorite.Id)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PageQuickAccessDto>> ListRecentAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = NormalizePage(pageNumber);
        pageSize = NormalizeSize(pageSize);

        return await (
                from recent in _context.PageRecents.AsNoTracking()
                where recent.UserId == userId
                join page in _context.Pages.AsNoTracking().Where(x => !x.IsArchived)
                    on recent.PageId equals page.Id
                join workspace in _context.Workspaces.AsNoTracking()
                    on page.WorkspaceId equals workspace.Id
                where workspace.OwnerId == userId
                      || workspace.Visibility == WorkspaceVisibility.Public
                      || _context.WorkspaceMembers.Any(member =>
                          member.WorkspaceId == workspace.Id && member.UserId == userId)
                orderby recent.LastVisitedAtUtc descending
                select new PageQuickAccessDto(
                    page.Id,
                    page.WorkspaceId,
                    page.ParentPageId,
                    workspace.Name,
                    page.Title,
                    page.Icon,
                    page.CoverImage,
                    page.IsArchived,
                    page.CurrentRevision,
                    page.CreatedDate,
                    page.UpdatedDate,
                    _context.PageFavorites.Any(favorite => favorite.UserId == userId && favorite.PageId == page.Id),
                    _context.PageFavorites
                        .Where(favorite => favorite.UserId == userId && favorite.PageId == page.Id)
                        .Select(favorite => (DateTimeOffset?)favorite.CreatedDate)
                        .FirstOrDefault(),
                    recent.LastVisitedAtUtc,
                    recent.VisitCount))
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountRecentAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return (
                from recent in _context.PageRecents.AsNoTracking()
                where recent.UserId == userId
                join page in _context.Pages.AsNoTracking().Where(x => !x.IsArchived)
                    on recent.PageId equals page.Id
                join workspace in _context.Workspaces.AsNoTracking()
                    on page.WorkspaceId equals workspace.Id
                where workspace.OwnerId == userId
                      || workspace.Visibility == WorkspaceVisibility.Public
                      || _context.WorkspaceMembers.Any(member =>
                          member.WorkspaceId == workspace.Id && member.UserId == userId)
                select recent.Id)
            .CountAsync(cancellationToken);
    }

    public void AddFavorite(PageFavorite favorite) => _context.PageFavorites.Add(favorite);

    public void RemoveFavorite(PageFavorite favorite) => _context.PageFavorites.Remove(favorite);

    public void AddRecent(PageRecent recent) => _context.PageRecents.Add(recent);

    private static int NormalizePage(int pageNumber) => pageNumber <= 0 ? 1 : pageNumber;

    private static int NormalizeSize(int pageSize) => pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
}
