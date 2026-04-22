using Microsoft.Extensions.Logging;
using Pkm.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Cache;

internal sealed class FallbackRedisCache : IRedisCache
{
    private readonly RedisCache _primary;
    private readonly InMemoryCache _fallback;
    private readonly ILogger<FallbackRedisCache> _logger;

    public FallbackRedisCache(
        RedisCache primary,
        InMemoryCache fallback,
        ILogger<FallbackRedisCache> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetAsync<T>(key, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis unavailable during GET for key {CacheKey}. Falling back to in-memory cache.", key);
            return await _fallback.GetAsync<T>(key, cancellationToken);
        }
    }

    public async Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _primary.SetAsync(key, value, ttl, cancellationToken);
            await _fallback.SetAsync(key, value, ttl, cancellationToken);
            return result;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis unavailable during SET for key {CacheKey}. Falling back to in-memory cache.", key);
            return await _fallback.SetAsync(key, value, ttl, cancellationToken);
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _primary.RemoveAsync(key, cancellationToken);
            await _fallback.RemoveAsync(key, cancellationToken);
            return result;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis unavailable during REMOVE for key {CacheKey}. Falling back to in-memory cache.", key);
            return await _fallback.RemoveAsync(key, cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.ExistsAsync(key, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis unavailable during EXISTS for key {CacheKey}. Falling back to in-memory cache.", key);
            return await _fallback.ExistsAsync(key, cancellationToken);
        }
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _primary.ExpireAsync(key, ttl, cancellationToken);
            await _fallback.ExpireAsync(key, ttl, cancellationToken);
            return result;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(ex, "Redis unavailable during EXPIRE for key {CacheKey}. Falling back to in-memory cache.", key);
            return await _fallback.ExpireAsync(key, ttl, cancellationToken);
        }
    }

    private static bool IsRedisFailure(Exception ex)
        => ex is RedisConnectionException
           or RedisTimeoutException
           or RedisServerException
           or TimeoutException
           or ObjectDisposedException;
}