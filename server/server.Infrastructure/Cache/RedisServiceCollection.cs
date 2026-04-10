using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace server.Infrastructure.Cache
{
    public static class RedisServiceCollection
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConn = configuration?.GetValue<string>("Redis:Connection");
            if (string.IsNullOrWhiteSpace(redisConn))
            {
                // Fallback to local SignalR if no Redis
                services.AddSignalR();
                return services;
            }

            var multiplexer = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<IConnectionMultiplexer>(sp => multiplexer.Value);
            services.AddSingleton<IRedisCacheService, RedisCacheService>();

            // Setup SignalR with Redis Backplane
            services.AddSignalR().AddStackExchangeRedis(redisConn);

            return services;
        }
    }
}
