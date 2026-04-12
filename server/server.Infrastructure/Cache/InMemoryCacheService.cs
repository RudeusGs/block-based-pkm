using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using server.Domain.Caching;

namespace server.Infrastructure.Cache;

/// <summary>
/// Process-local cache used when Redis is unavailable or not configured.
/// </summary>
public sealed class InMemoryCacheService : IRedisCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _store = new();
    private readonly ILogger<InMemoryCacheService>? _logger;

    public InMemoryCacheService(ILogger<InMemoryCacheService>? logger = null)
    {
        _logger = logger;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            _store[key] = new CacheEntry(json, expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InMemory cache SetAsync failed for key {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        try
        {
            if (!_store.TryGetValue(key, out var entry) || entry.IsExpired)
            {
                if (entry.IsExpired)
                    _store.TryRemove(key, out _);
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult(JsonSerializer.Deserialize<T>(entry.Payload));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InMemory cache GetAsync failed for key {Key}", key);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            _store[key] = new CacheEntry(value, expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : null);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InMemory cache SetStringAsync failed for key {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task<string?> GetStringAsync(string key)
    {
        try
        {
            if (!_store.TryGetValue(key, out var entry) || entry.IsExpired)
            {
                if (entry.IsExpired)
                    _store.TryRemove(key, out _);
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(entry.Payload);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "InMemory cache GetStringAsync failed for key {Key}", key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task RemoveAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    private readonly record struct CacheEntry(string Payload, DateTime? ExpiresAtUtc)
    {
        public bool IsExpired => ExpiresAtUtc.HasValue && DateTime.UtcNow >= ExpiresAtUtc.Value;
    }
}
