using System.Collections.Concurrent;
using server.Domain.Realtime;

namespace server.Infrastructure.Realtime.Services;
public sealed class InMemoryPresenceService : IPresenceService
{
    private readonly ConcurrentDictionary<string, PresenceUserModel> _pagePresence = new();
    private readonly ConcurrentDictionary<string, int> _blockLocks = new();
    private static readonly TimeSpan PresenceTtl = TimeSpan.FromSeconds(30);

    public Task HeartbeatPageAsync(int pageId, int userId, string userName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var userKey = PresenceKey(pageId, userId);
        _pagePresence[userKey] = new PresenceUserModel
        {
            UserId = userId,
            UserName = userName,
            LastActiveAt = DateTime.UtcNow
        };
        return Task.CompletedTask;
    }

    public Task<List<PresenceUserModel>> GetActiveUsersOnPageAsync(int pageId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var prefix = $"Presence:Page:{pageId}:User:";
        var now = DateTime.UtcNow;
        var users = new List<PresenceUserModel>();

        foreach (var kv in _pagePresence)
        {
            ct.ThrowIfCancellationRequested();
            if (!kv.Key.StartsWith(prefix, StringComparison.Ordinal))
                continue;
            if (now - kv.Value.LastActiveAt > PresenceTtl)
            {
                _pagePresence.TryRemove(kv.Key, out _);
                continue;
            }

            users.Add(kv.Value);
        }

        return Task.FromResult(users);
    }

    public Task<bool> AcquireBlockLockAsync(int blockId, int userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = BlockLockKey(blockId);
        if (_blockLocks.TryGetValue(key, out var lockedBy))
        {
            if (lockedBy == userId)
                return Task.FromResult(true);
            return Task.FromResult(false);
        }

        if (_blockLocks.TryAdd(key, userId))
            return Task.FromResult(true);

        if (_blockLocks.TryGetValue(key, out lockedBy) && lockedBy == userId)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public Task ReleaseBlockLockAsync(int blockId, int userId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var key = BlockLockKey(blockId);
        if (_blockLocks.TryGetValue(key, out var lockedBy) && lockedBy == userId)
            _blockLocks.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private static string PresenceKey(int pageId, int userId) => $"Presence:Page:{pageId}:User:{userId}";

    private static string BlockLockKey(int blockId) => $"Lock:Block:{blockId}";
}
