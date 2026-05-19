using Microsoft.Extensions.Logging;
using Pkm.Application.Abstractions.Caching;

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache GET failed for key {CacheKey}. Falling back to in-memory cache.",
                key);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache SET failed for key {CacheKey}. Falling back to in-memory cache.",
                key);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache REMOVE failed for key {CacheKey}. Falling back to in-memory cache.",
                key);

            return await _fallback.RemoveAsync(key, cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.ExistsAsync(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache EXISTS failed for key {CacheKey}. Falling back to in-memory cache.",
                key);

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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Cache EXPIRE failed for key {CacheKey}. Falling back to in-memory cache.",
                key);

            return await _fallback.ExpireAsync(key, ttl, cancellationToken);
        }
    }
}