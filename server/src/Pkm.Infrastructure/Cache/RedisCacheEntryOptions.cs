namespace Pkm.Infrastructure.Cache;

public sealed class RedisCacheEntryOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; init; }
}