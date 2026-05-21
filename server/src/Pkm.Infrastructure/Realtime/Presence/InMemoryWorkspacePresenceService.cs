using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;

namespace Pkm.Infrastructure.Realtime.Presence;

public sealed class InMemoryWorkspacePresenceService : IWorkspacePresenceService
{
    private readonly ConcurrentDictionary<string, WorkspacePresenceEntry> _connections = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly TimeSpan _ttl;

    public InMemoryWorkspacePresenceService(
        IClock clock,
        IOptions<RealtimeOptions> options)
    {
        _clock = clock;
        _ttl = TimeSpan.FromSeconds(Math.Max(5, options.Value.PresenceTtlSeconds));
    }

    public Task<WorkspacePresenceEntry> UpsertAsync(
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var entry = new WorkspacePresenceEntry(
            WorkspaceId: workspaceId,
            UserId: userId,
            UserName: userName,
            ConnectionId: connectionId,
            LastSeenUtc: _clock.UtcNow);

        _connections[Key(workspaceId, connectionId)] = entry;
        CleanupExpired();

        return Task.FromResult(entry);
    }

    public Task<WorkspacePresenceEntry?> RemoveConnectionFromWorkspaceAsync(
        Guid workspaceId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        _connections.TryRemove(Key(workspaceId, connectionId), out var removed);
        return Task.FromResult<WorkspacePresenceEntry?>(removed);
    }

    public Task<IReadOnlyList<WorkspacePresenceEntry>> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var removed = new List<WorkspacePresenceEntry>();

        foreach (var pair in _connections)
        {
            if (!string.Equals(pair.Value.ConnectionId, connectionId, StringComparison.Ordinal))
                continue;

            if (_connections.TryRemove(pair.Key, out var entry))
            {
                removed.Add(entry);
            }
        }

        return Task.FromResult<IReadOnlyList<WorkspacePresenceEntry>>(removed);
    }

    public Task<IReadOnlyList<WorkspacePresenceEntry>> GetActiveOnWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        CleanupExpired();

        var now = _clock.UtcNow;
        var active = _connections.Values
            .Where(x => x.WorkspaceId == workspaceId && now - x.LastSeenUtc <= _ttl)
            .OrderByDescending(x => x.LastSeenUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyList<WorkspacePresenceEntry>>(active);
    }

    private void CleanupExpired()
    {
        var threshold = _clock.UtcNow - _ttl;

        foreach (var pair in _connections)
        {
            if (pair.Value.LastSeenUtc < threshold)
            {
                _connections.TryRemove(pair.Key, out _);
            }
        }
    }

    private static string Key(Guid workspaceId, string connectionId)
        => $"{workspaceId:D}:{connectionId}";
}