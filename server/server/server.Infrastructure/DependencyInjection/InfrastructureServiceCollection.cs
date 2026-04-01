using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using server.Infrastructure.Cache;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices();
        services.AddJwtAuthentication(configuration);
        services.AddRedis(configuration);

        return services;
    }
}