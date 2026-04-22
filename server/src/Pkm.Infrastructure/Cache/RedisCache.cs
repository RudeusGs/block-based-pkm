using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Cache;

internal sealed class RedisCache : IRedisCache
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IRedisSerializer _serializer;
    private readonly RedisOptions _options;

    public RedisCache(
        IConnectionMultiplexer connectionMultiplexer,
        IRedisSerializer serializer,
        IOptions<RedisOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _serializer = serializer;
        _options = options.Value;
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase(_options.DefaultDatabase);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await Database.StringGetAsync(key);
        if (!value.HasValue)
            return default;

        return _serializer.Deserialize<T>(value!);
    }

    public async Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var payload = _serializer.Serialize(value);
        var effectiveTtl = ttl ?? TimeSpan.FromMinutes(_options.DefaultTtlMinutes);

        return await Database.StringSetAsync(key, payload, effectiveTtl);
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return await Database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await Database.KeyExistsAsync(key);
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        return await Database.KeyExpireAsync(key, ttl);
    }
}