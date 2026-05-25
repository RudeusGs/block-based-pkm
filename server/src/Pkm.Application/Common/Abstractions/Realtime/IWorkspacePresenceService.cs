namespace Pkm.Application.Common.Abstractions.Realtime;

public interface IWorkspacePresenceService
{
    Task<WorkspacePresenceEntry> UpsertAsync(
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<WorkspacePresenceEntry?> RemoveConnectionFromWorkspaceAsync(
        Guid workspaceId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspacePresenceEntry>> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkspacePresenceEntry>> GetActiveOnWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
