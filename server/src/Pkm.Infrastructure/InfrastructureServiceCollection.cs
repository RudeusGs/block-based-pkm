using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pkm.Infrastructure.Authentication;
using Pkm.Infrastructure.Cache;
using Pkm.Infrastructure.Email;
using Pkm.Infrastructure.Persistence;
using Pkm.Infrastructure.Realtime;
using Pkm.Infrastructure.Storage;
using Pkm.Infrastructure.Time;

namespace Pkm.Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistenceInfrastructure(configuration);
        services.AddAuthenticationInfrastructure(configuration);
        services.AddCacheInfrastructure(configuration);
        services.AddEmailInfrastructure(configuration);
        services.AddRealtimeInfrastructure(configuration);
        services.AddTimeInfrastructure();
        services.AddStorageInfrastructure(configuration);

        return services;
    }
}
