namespace Pkm.Application.Abstractions.Realtime;

public interface IPagePresenceService
{
    Task<PagePresenceEntry> UpsertAsync(
        Guid pageId,
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<PagePresenceEntry?> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PagePresenceEntry>> GetActiveOnPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default);
}