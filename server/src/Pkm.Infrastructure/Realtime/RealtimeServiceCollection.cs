using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Application.Abstractions.Realtime;
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

        services.AddSignalR();
        services.AddSingleton<InMemoryPagePresenceService>();
        services.AddSingleton<InMemoryBlockEditLeaseService>();

        var hasRedisMultiplexer = services.Any(
            descriptor => descriptor.ServiceType == typeof(IConnectionMultiplexer));

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
        return services;
    }
}