namespace server.Domain.Caching;

/// <summary>
/// Abstraction for distributed or in-process cache used by application services.
/// Infrastructure supplies Redis or in-memory implementations.
/// </summary>
public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    Task<T?> GetAsync<T>(string key);

    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);

    Task<string?> GetStringAsync(string key);

    Task RemoveAsync(string key);
}
