using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;

namespace Pkm.Infrastructure.Realtime.Presence;

public sealed class InMemoryPagePresenceService : IPagePresenceService
{
    private readonly ConcurrentDictionary<string, PagePresenceEntry> _connections = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly TimeSpan _ttl;

    public InMemoryPagePresenceService(
        IClock clock,
        IOptions<RealtimeOptions> options)
    {
        _clock = clock;
        _ttl = TimeSpan.FromSeconds(Math.Max(5, options.Value.PresenceTtlSeconds));
    }

    public Task<PagePresenceEntry> UpsertAsync(
        Guid pageId,
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var entry = new PagePresenceEntry(
            PageId: pageId,
            WorkspaceId: workspaceId,
            UserId: userId,
            UserName: userName,
            ConnectionId: connectionId,
            LastSeenUtc: _clock.UtcNow);

        _connections[connectionId] = entry;
        CleanupExpired();

        return Task.FromResult(entry);
    }

    public Task<PagePresenceEntry?> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        _connections.TryRemove(connectionId, out var removed);
        return Task.FromResult<PagePresenceEntry?>(removed);
    }

    public Task<IReadOnlyList<PagePresenceEntry>> GetActiveOnPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        CleanupExpired();

        var now = _clock.UtcNow;
        var active = _connections.Values
            .Where(x => x.PageId == pageId && now - x.LastSeenUtc <= _ttl)
            .OrderByDescending(x => x.LastSeenUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PagePresenceEntry>>(active);
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
}