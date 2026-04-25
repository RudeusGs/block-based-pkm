using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Infrastructure.Cache;
using Pkm.Infrastructure.Realtime.Locks;
using Pkm.Infrastructure.Realtime.Presence;
using Pkm.Infrastructure.Realtime.Publishers;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime;

public static class RealtimeServiceCollection
{
    public static IServiceCollection AddRealtimeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RealtimeOptions>(
            configuration.GetSection(RealtimeOptions.SectionName));

        var realtimeOptions = configuration
            .GetSection(RealtimeOptions.SectionName)
            .Get<RealtimeOptions>()
            ?? new RealtimeOptions();

        var redisOptions = configuration
            .GetSection(RedisOptions.SectionName)
            .Get<RedisOptions>()
            ?? new RedisOptions();

        var hasRedisMultiplexer = services.Any(
            descriptor => descriptor.ServiceType == typeof(IConnectionMultiplexer));

        var signalRBuilder = services.AddSignalR();

        if (realtimeOptions.UseRedisBackplane &&
            hasRedisMultiplexer &&
            !string.IsNullOrWhiteSpace(redisOptions.Connection))
        {
            var channelPrefix = string.IsNullOrWhiteSpace(realtimeOptions.RedisBackplaneChannelPrefix)
                ? $"{redisOptions.InstanceName}:signalr"
                : realtimeOptions.RedisBackplaneChannelPrefix;

            signalRBuilder.AddStackExchangeRedis(
                redisOptions.Connection,
                options =>
                {
                    options.Configuration.ChannelPrefix = RedisChannel.Literal(channelPrefix);
                });
        }

        services.AddSingleton<InMemoryPagePresenceService>();
        services.AddSingleton<InMemoryBlockEditLeaseService>();

        if (hasRedisMultiplexer)
        {
            services.AddSingleton<RedisPagePresenceService>();
            services.AddSingleton<RedisBlockEditLeaseService>();

            services.AddSingleton<IPagePresenceService, FallbackPagePresenceService>();
            services.AddSingleton<IBlockEditLeaseService, FallbackBlockEditLeaseService>();
        }
        else
        {
            services.AddSingleton<IPagePresenceService>(
                sp => sp.GetRequiredService<InMemoryPagePresenceService>());

            services.AddSingleton<IBlockEditLeaseService>(
                sp => sp.GetRequiredService<InMemoryBlockEditLeaseService>());
        }

        services.AddSingleton<IDocumentRealtimePublisher, SignalRDocumentRealtimePublisher>();
        services.AddSingleton<ITaskRealtimePublisher, SignalRTaskRealtimePublisher>();
        services.AddSingleton<INotificationRealtimePublisher, SignalRNotificationRealtimePublisher>();
        services.AddSingleton<IRecommendationRealtimePublisher, SignalRRecommendationRealtimePublisher>();

        return services;
    }
}