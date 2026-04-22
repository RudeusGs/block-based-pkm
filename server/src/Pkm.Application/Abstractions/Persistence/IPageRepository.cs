using Pkm.Application.Features.Pages.Models;
using Pkm.Domain.Pages;

namespace Pkm.Application.Abstractions.Persistence;

public interface IPageRepository
{
    Task<Page?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Page?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Page>> ListByWorkspaceAsync(
        Guid workspaceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Page>> ListSubPagesAsync(
        Guid parentPageId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Page>> SearchInWorkspaceAsync(
        Guid workspaceId,
        string keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountSearchInWorkspaceAsync(
        Guid workspaceId,
        string keyword,
        CancellationToken cancellationToken = default);

    Task<PageAccessContextReadModel?> GetAccessContextAsync(
        Guid pageId,
        Guid userId,
        CancellationToken cancellationToken = default);

    void Add(Page page);
}