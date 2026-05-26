namespace Pkm.Application.Features.Pages.Services;

/// <summary>
/// Cleans up pages that stayed in Trash past the configured retention window.
/// </summary>
public interface IPageTrashCleanupService
{
    Task<int> CleanupExpiredArchivedPagesAsync(
        TimeSpan retention,
        int batchSize,
        CancellationToken cancellationToken = default);
}
