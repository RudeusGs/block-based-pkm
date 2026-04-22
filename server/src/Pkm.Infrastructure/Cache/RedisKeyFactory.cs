using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Caching;

namespace Pkm.Infrastructure.Cache;

internal sealed class RedisKeyFactory : IRedisKeyFactory
{
    private readonly RedisOptions _options;

    public RedisKeyFactory(IOptions<RedisOptions> options)
    {
        _options = options.Value;
    }

    public string Build(params string[] segments)
    {
        var sanitizedSegments = segments
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().Replace(" ", "_").ToLowerInvariant());

        return string.Join(":", new[] { _options.InstanceName, "v1" }.Concat(sanitizedSegments));
    }
}