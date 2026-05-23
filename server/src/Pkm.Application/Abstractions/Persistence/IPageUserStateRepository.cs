using Pkm.Application.Features.Pages.Models;
using Pkm.Domain.Pages;

namespace Pkm.Application.Abstractions.Persistence;

public interface IPageUserStateRepository
{
    Task<PageFavorite?> GetFavoriteAsync(
        Guid userId,
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<PageRecent?> GetRecentAsync(
        Guid userId,
        Guid pageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PageQuickAccessDto>> ListFavoritesAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountFavoritesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PageQuickAccessDto>> ListRecentAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountRecentAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    void AddFavorite(PageFavorite favorite);

    void RemoveFavorite(PageFavorite favorite);

    void AddRecent(PageRecent recent);
}
