using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace server.Infrastructure;

/// <summary>
/// Cross-cutting infrastructure that depends on persistence (Identity, JWT) but not on Redis or SignalR.
/// </summary>
public static class CoreInfrastructureServiceCollection
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        IdentityServiceCollection.AddIdentityServices(services);
        AuthenticationServiceCollection.AddJwtAuthentication(services, configuration);
        return services;
    }
}
