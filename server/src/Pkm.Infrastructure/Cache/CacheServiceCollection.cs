using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Cache;

public static class CacheServiceCollection
{
    public static IServiceCollection AddCacheInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        services.AddSingleton<IRedisSerializer, SystemTextJsonRedisSerializer>();
        services.AddSingleton<IRedisKeyFactory, RedisKeyFactory>();
        services.AddSingleton<InMemoryCache>();

        var redisOptions = configuration
            .GetSection(RedisOptions.SectionName)
            .Get<RedisOptions>()
            ?? new RedisOptions();

        if (string.IsNullOrWhiteSpace(redisOptions.Connection))
        {
            services.AddScoped<IRedisCache>(sp => sp.GetRequiredService<InMemoryCache>());
            return services;
        }

        try
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisOptions.Connection);

            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            services.AddScoped<RedisCache>();
            services.AddScoped<IRedisCache, FallbackRedisCache>();

            return services;
        }
        catch
        {
            services.AddScoped<IRedisCache>(sp => sp.GetRequiredService<InMemoryCache>());
            return services;
        }
    }
}