namespace Pkm.Application.Abstractions.Caching;

public interface IRedisCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task<bool> SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExpireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);
}