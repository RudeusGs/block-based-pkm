using System.Collections.Concurrent;
using Pkm.Application.Abstractions.Caching;

namespace Pkm.Infrastructure.Cache;

internal sealed class InMemoryCache : IRedisCache
{
    private sealed record CacheEntry(object? Value, DateTimeOffset? ExpiresAtUtc);

    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return Task.FromResult<T?>(default);

        if (entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult<T?>(default);
        }

        return Task.FromResult(entry.Value is T typed ? typed : default);
    }

    public Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset? expiresAtUtc = ttl.HasValue
            ? DateTimeOffset.UtcNow.Add(ttl.Value)
            : null;

        _entries[key] = new CacheEntry(value, expiresAtUtc);
        return Task.FromResult(true);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var removed = _entries.TryRemove(key, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return Task.FromResult(false);

        if (entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task<bool> ExpireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(key, out var entry))
            return Task.FromResult(false);

        _entries[key] = entry with
        {
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(ttl)
        };

        return Task.FromResult(true);
    }
}