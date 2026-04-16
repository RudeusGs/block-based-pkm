namespace server.Domain.Caching;

public interface IRedisCacheService
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    Task<T?> GetAsync<T>(string key);

    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);

    Task<string?> GetStringAsync(string key);

    Task RemoveAsync(string key);
}
