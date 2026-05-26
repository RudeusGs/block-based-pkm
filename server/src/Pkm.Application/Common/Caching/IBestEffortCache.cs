namespace Pkm.Application.Common.Caching;

public interface IBestEffortCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);
}
