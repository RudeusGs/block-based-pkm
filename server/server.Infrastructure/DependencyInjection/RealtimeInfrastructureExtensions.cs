using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Domain.Caching;
using server.Domain.Realtime;
using server.Infrastructure.Cache;
using server.Infrastructure.Realtime;
using server.Infrastructure.Realtime.Services;
using StackExchange.Redis;

namespace server.Infrastructure;

/// <summary>
/// SignalR, optional Redis backplane, cache, and presence. Safe when Redis is missing or unreachable.
/// </summary>
public static class RealtimeInfrastructureExtensions
{
    public static IServiceCollection AddRealtimeInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConn = configuration["Redis:Connection"]?.Trim();
        var signalRBuilder = services.AddSignalR();

        if (TryCreateRedisMultiplexer(redisConn, out var mux) && mux is not null)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => mux);
            services.AddSingleton<IRedisCacheService, RedisCacheService>();
            services.AddSingleton<IPresenceService, PresenceService>();
            signalRBuilder.AddStackExchangeRedis(redisConn!);
        }
        else
        {
            services.AddSingleton<IRedisCacheService, InMemoryCacheService>();
            services.AddSingleton<IPresenceService, InMemoryPresenceService>();
        }

        services.AddSingleton<IUserIdProvider, NameIdentifierUserIdProvider>();
        services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();
        return services;
    }

    /// <summary>
    /// Returns true when a multiplexer was created. Missing connection string returns false without throwing.
    /// </summary>
    private static bool TryCreateRedisMultiplexer(string? redisConn, out IConnectionMultiplexer? multiplexer)
    {
        multiplexer = null;

        if (string.IsNullOrWhiteSpace(redisConn))
            return false;

        IConnectionMultiplexer? mux = null;
        try
        {
            var options = ConfigurationOptions.Parse(redisConn);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 3000;
            mux = ConnectionMultiplexer.Connect(options);
            mux.GetDatabase().Ping();
            multiplexer = mux;
            return true;
        }
        catch
        {
            try { mux?.Dispose(); } catch { /* ignore */ }
            multiplexer = null;
            return false;
        }
    }
}
