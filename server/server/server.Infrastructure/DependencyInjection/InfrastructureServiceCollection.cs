using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Infrastructure.Cache;
using server.Infrastructure.Realtime;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices();
        services.AddJwtAuthentication(configuration);
        services.AddRedis(configuration);
        // Realtime SignalR DI registration (hubs, user id provider)
        services.AddRealtime(configuration);

        return services;
    }
}